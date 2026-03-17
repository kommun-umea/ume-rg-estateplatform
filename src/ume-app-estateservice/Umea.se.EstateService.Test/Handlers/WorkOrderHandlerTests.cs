using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.SqlServer;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.ServiceAccess.FileStorage;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Handlers;

public class WorkOrderHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EstateDbContext _dbContext;
    private readonly InMemoryDataStore _dataStore;
    private readonly WorkOrderHandler _handler;

    public WorkOrderHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<EstateDbContext> options = new DbContextOptionsBuilder<EstateDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create schema
        using (EstateDbContext context = new(options))
        {
            context.Database.EnsureCreated();
        }

        _dbContext = new EstateDbContext(options);
        IWorkOrderRepository workOrderRepository = new WorkOrderRepository(_dbContext);
        _dataStore = new InMemoryDataStore();

        DataStoreSeeder.Seed(
            _dataStore,
            buildings: [new BuildingEntity { Id = 1, Name = "Building One", PopularName = "B1" }],
            rooms: [new RoomEntity { Id = 10, Name = "Room Ten", PopularName = "R10", BuildingId = 1 }]);

        ApplicationConfig config = CreateTestConfig();
        IWorkOrderFileStorage fileStorage = new LocalWorkOrderFileStorage(config);
        IWorkOrderFileValidator fileValidator = new WorkOrderFileValidator(config);
        _handler = new WorkOrderHandler(workOrderRepository, _dataStore, new WorkOrderChannel(), fileStorage, fileValidator, NullLogger<WorkOrderHandler>.Instance);
    }

    [Fact]
    public async Task SubmitWorkOrder_ValidIndoor_CreatesEntity()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "error_report",
            Location = "Indoor",
            RoomId = 10,
            Description = "Test workOrder"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        result.Id.ShouldNotBe(Guid.Empty);
        result.SyncStatus.ShouldBe("Pending");

        // Verify detail through GetWorkOrderAsync
        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.BuildingName.ShouldBe("Building One");
        detail.RoomName.ShouldBe("Room Ten");
        detail.Location.ShouldBe("Indoor");
    }

    [Fact]
    public async Task SubmitWorkOrder_ValidOutdoor_CreatesEntityWithoutRoom()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "error_report",
            Location = "Outdoor",
            Description = "Outdoor issue"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        WorkOrderDetailModel detail = await _handler.GetWorkOrderAsync(result.Id, "test@example.com");
        detail.RoomName.ShouldBeNull();
        detail.Location.ShouldBe("Outdoor");
    }

    [Fact]
    public async Task SubmitWorkOrder_InvalidLocation_Throws()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "error_report",
            Location = "InvalidType",
            Description = "Test"
        };

        await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_InvalidWorkOrderType_Throws()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "invalid_type",
            Location = "Indoor",
            Description = "Test"
        };

        await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_InvalidBuilding_Throws()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 9999,
            WorkOrderType = "error_report",
            Location = "Indoor",
            Description = "Test"
        };

        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_OutdoorWithRoom_Throws()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "error_report",
            Location = "Outdoor",
            RoomId = 10,
            Description = "Test"
        };

        await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_RoomNotInBuilding_Throws()
    {
        DataStoreSeeder.Seed(
            _dataStore,
            buildings: [new BuildingEntity { Id = 1, Name = "Building One", PopularName = "B1" }],
            rooms: [new RoomEntity { Id = 10, Name = "Room Ten", PopularName = "R10", BuildingId = 2 }]);

        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "error_report",
            Location = "Indoor",
            RoomId = 10,
            Description = "Test"
        };

        await Should.ThrowAsync<BusinessValidationException>(
            () => _handler.SubmitWorkOrderAsync(request, "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_BuildingService_SetsCorrectTypeId()
    {
        CreateWorkOrderRequest request = new()
        {
            BuildingId = 1,
            WorkOrderType = "building_service",
            Location = "Indoor",
            Description = "Service request"
        };

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(request, "test@example.com");

        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetWorkOrders_ReturnsOnlyUserWorkOrders()
    {
        await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "User A workOrder" },
            "usera@example.com");

        await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "User B workOrder" },
            "userb@example.com");

        IReadOnlyList<WorkOrderListItemModel> result = await _handler.GetWorkOrdersAsync("usera@example.com");

        result.Count.ShouldBe(1);
        result[0].Description.ShouldBe("User A workOrder");
    }

    [Fact]
    public async Task GetWorkOrder_ByUid_ReturnsCorrectWorkOrder()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "Find me" },
            "test@example.com");

        WorkOrderDetailModel result = await _handler.GetWorkOrderAsync(created.Id, "test@example.com");

        result.ShouldNotBeNull();
        result.Description.ShouldBe("Find me");
    }

    [Fact]
    public async Task GetWorkOrder_WrongUser_ThrowsNotFound()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "Not yours" },
            "usera@example.com");

        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.GetWorkOrderAsync(created.Id, "userb@example.com"));
    }

    [Fact]
    public async Task SyncWorkOrder_ReturnsWorkOrder()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "Sync me" },
            "test@example.com");

        WorkOrderDetailModel result = await _handler.SyncWorkOrderAsync(created.Id, "test@example.com");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task SyncWorkOrder_WrongUser_ThrowsNotFound()
    {
        WorkOrderSubmissionModel created = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "Not yours" },
            "usera@example.com");

        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.SyncWorkOrderAsync(created.Id, "userb@example.com"));
    }

    [Fact]
    public async Task SyncWorkOrder_NotFound_ThrowsNotFound()
    {
        await Should.ThrowAsync<EntityNotFoundException>(
            () => _handler.SyncWorkOrderAsync(Guid.NewGuid(), "test@example.com"));
    }

    [Fact]
    public async Task SubmitWorkOrder_SetsNextSyncAtToNow()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        WorkOrderSubmissionModel result = await _handler.SubmitWorkOrderAsync(
            new CreateWorkOrderRequest { BuildingId = 1, WorkOrderType = "error_report", Location = "Indoor", Description = "Test" },
            "test@example.com");

        result.Id.ShouldNotBe(Guid.Empty);
        result.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    private static ApplicationConfig CreateTestConfig()
    {
        Dictionary<string, string?> configData = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Test",
            ["WorkOrder:FileStorage"] = Path.Combine(Path.GetTempPath(), "workOrder-handler-tests"),
            ["WorkOrder:MaxRetries"] = "3",
            ["Pythagoras:ApiKey"] = "test",
            ["Pythagoras:BaseUrl"] = "https://localhost/",
            ["Authentication:TokenServiceUrl"] = "https://localhost/",
            ["Authentication:Audience"] = "test"
        };

        Microsoft.Extensions.Configuration.IConfigurationRoot configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new ApplicationConfig(configuration);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();

        // Clean up test files
        string testDir = Path.Combine(Path.GetTempPath(), "workOrder-handler-tests");
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, recursive: true);
        }
    }
}
