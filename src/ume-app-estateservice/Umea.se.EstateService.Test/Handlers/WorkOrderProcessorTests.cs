using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.SqlServer;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Test.TestHelpers;

namespace Umea.se.EstateService.Test.Handlers;

/// <summary>
/// Unit tests for WorkOrderProcessor focused on the outgoing Pythagoras create payload.
/// These exist because Pythagoras rejects creates when its per-type WorkOrderFieldSetting
/// declares a field MANDATORY_WHEN_CREATED and the payload omits it. The processor is
/// responsible for filling those fields from classifier output or configured defaults.
/// </summary>
public class WorkOrderProcessorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly EstateDbContext _dbContext;
    private readonly IWorkOrderRepository _repository;
    private readonly FakePythagorasClient _fakeClient;
    private readonly StubCategoryClassifier _classifier;
    private readonly StubFileStorage _fileStorage;
    private readonly StubStatusSyncService _statusSync;
    private readonly WorkOrderProcessor _processor;

    public WorkOrderProcessorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<EstateDbContext> options = new DbContextOptionsBuilder<EstateDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (EstateDbContext context = new(options))
        {
            context.Database.EnsureCreated();
        }

        _dbContext = new EstateDbContext(options);
        _repository = new WorkOrderRepository(_dbContext);

        _fakeClient = new FakePythagorasClient();
        _fakeClient.SetCreateWorkOrderResult(new WorkOrderDto { Id = 555 });

        _classifier = new StubCategoryClassifier();
        _fileStorage = new StubFileStorage();
        _statusSync = new StubStatusSyncService();

        _processor = new WorkOrderProcessor(
            _repository,
            _fakeClient,
            _statusSync,
            _classifier,
            _fileStorage,
            CreateConfig(),
            NullLogger<WorkOrderProcessor>.Instance);
    }

    [Fact]
    public async Task BuildingService_NoClassifierMatch_UsesConfiguredDefaultCategoryId()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([]); // classifier gives nothing

        await _processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        payload.CategoryId.ShouldBe(82); // DefaultCategoryIdByType[2] from config
        payload.OperatingGroupId.ShouldBeNull(); // not required for BuildingService
    }

    [Fact]
    public async Task BuildingService_FallbackCategory_IsNotPersistedOnEntity()
    {
        // Invariant: the configured fallback goes into the outgoing Pythagoras payload only.
        // workOrder.CategoryId must remain null so stored data reflects the classifier's
        // actual decision (here: no suggestion), not a fabricated one.
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([]);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem().CategoryId.ShouldBe(82);
        WorkOrderEntity reloaded = (await _repository.GetByUidAsync(workOrder.Uid, workOrder.CreatedByEmail))!;
        reloaded.CategoryId.ShouldBeNull();
    }

    [Fact]
    public async Task BuildingService_LowConfidenceClassifierHit_IsNotPersistedOnEntity()
    {
        // Invariant: below-threshold classifier picks are ignored end-to-end — neither
        // written to the entity nor sent to Pythagoras as a classifier decision.
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([new WorkOrderCategorySuggestion { CategoryId = 84, CategoryName = "Miljötillsyn", Confidence = 0.2 }]);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        WorkOrderEntity reloaded = (await _repository.GetByUidAsync(workOrder.Uid, workOrder.CreatedByEmail))!;
        reloaded.CategoryId.ShouldBeNull();
    }

    [Fact]
    public async Task BuildingService_HighConfidenceClassifierHit_IsPersistedOnEntity()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([new WorkOrderCategorySuggestion { CategoryId = 83, CategoryName = "Utemiljö", Confidence = 0.9 }]);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        WorkOrderEntity reloaded = (await _repository.GetByUidAsync(workOrder.Uid, workOrder.CreatedByEmail))!;
        reloaded.CategoryId.ShouldBe(83);
    }

    [Fact]
    public async Task BuildingService_ClassifierHit_UsesClassifierCategory()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([new WorkOrderCategorySuggestion { CategoryId = 83, CategoryName = "Utemiljö", Confidence = 0.9 }]);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        payload.CategoryId.ShouldBe(83);
    }

    [Fact]
    public async Task BuildingService_LowConfidenceClassifierHit_FallsBackToConfiguredDefault()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([new WorkOrderCategorySuggestion { CategoryId = 84, CategoryName = "Miljötillsyn", Confidence = 0.2 }]);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        // 0.2 is below the 0.75 default threshold — the classifier pick is ignored in favor of the configured default.
        payload.CategoryId.ShouldBe(82);
    }

    [Fact]
    public async Task BuildingService_LoweredThreshold_AcceptsLowConfidenceClassifierHit()
    {
        ApplicationConfig config = CreateConfig(classifierThreshold: 0.1);
        WorkOrderProcessor processor = new(
            _repository, _fakeClient, _statusSync, _classifier, _fileStorage,
            config, NullLogger<WorkOrderProcessor>.Instance);

        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([new WorkOrderCategorySuggestion { CategoryId = 84, CategoryName = "Miljötillsyn", Confidence = 0.2 }]);

        await processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        payload.CategoryId.ShouldBe(84);
    }

    [Fact]
    public async Task ErrorReport_SuppliesConfiguredOperatingGroupId()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.ErrorReport);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        payload.OperatingGroupId.ShouldBe(16);
    }

    [Fact]
    public async Task FacilityService_SuppliesConfiguredOperatingGroupId()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.FacilityService);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        payload.OperatingGroupId.ShouldBe(21);
    }

    [Fact]
    public async Task TownHallService_SuppliesConfiguredOperatingGroupId()
    {
        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.TownHallService);

        await _processor.ProcessPendingAsync(CancellationToken.None);

        CreatePythagorasWorkOrderRequest payload = _fakeClient.CreateWorkOrderPayloads.ShouldHaveSingleItem();
        payload.OperatingGroupId.ShouldBe(22);
    }

    [Fact]
    public async Task BuildingService_NoClassifierHitNoDefault_MarksWorkOrderFailed()
    {
        // Rebuild processor with config missing the BuildingService default
        ApplicationConfig bareConfig = CreateConfig(includeBuildingServiceDefault: false);
        WorkOrderProcessor processor = new(
            _repository, _fakeClient, _statusSync, _classifier, _fileStorage,
            bareConfig, NullLogger<WorkOrderProcessor>.Instance);

        WorkOrderEntity workOrder = await SeedPendingAsync(PythagorasWorkOrderType.BuildingService);
        _classifier.SetSuggestions([]);

        await processor.ProcessPendingAsync(CancellationToken.None);

        _fakeClient.CreateWorkOrderPayloads.ShouldBeEmpty();

        WorkOrderEntity? reloaded = await _repository.GetByUidAsync(workOrder.Uid, workOrder.CreatedByEmail);
        reloaded.ShouldNotBeNull();
        reloaded.SyncStatus.ShouldBe(WorkOrderSyncStatus.Failed);
        reloaded.ErrorMessage.ShouldNotBeNull();
        reloaded.ErrorMessage.ShouldContain("DefaultCategoryIdByType");
    }

    private async Task<WorkOrderEntity> SeedPendingAsync(PythagorasWorkOrderType type)
    {
        WorkOrderEntity workOrder = new()
        {
            Uid = Guid.NewGuid(),
            BuildingId = 1933,
            BuildingName = "Test Building",
            Description = "Test",
            WorkOrderTypeId = (int)type,
            SyncStatus = WorkOrderSyncStatus.Pending,
            CreatedByEmail = "test@example.com",
            NotifierEmail = "test@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            NextSyncAt = DateTimeOffset.UtcNow.AddSeconds(-1),
        };
        await _repository.AddAsync(workOrder);
        return workOrder;
    }

    private static ApplicationConfig CreateConfig(bool includeBuildingServiceDefault = true, double? classifierThreshold = null)
    {
        Dictionary<string, string?> data = new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Test",
            ["WorkOrder:FileStorage"] = "./wo-proc-tests",
            ["WorkOrder:MaxRetries"] = "3",
            ["WorkOrder:RetryBaseDelaySeconds"] = "0",
            ["WorkOrder:ProcessingTimeoutMinutes"] = "10",
            ["WorkOrder:DefaultOperatingGroupIdByType:1"] = "16",
            ["WorkOrder:DefaultOperatingGroupIdByType:8"] = "21",
            ["WorkOrder:DefaultOperatingGroupIdByType:9"] = "22",
            ["Pythagoras:ApiKey"] = "test",
            ["Pythagoras:BaseUrl"] = "https://localhost/",
            ["Authentication:TokenServiceUrl"] = "https://localhost/",
            ["Authentication:Audience"] = "test",
        };

        if (includeBuildingServiceDefault)
        {
            data["WorkOrder:DefaultCategoryIdByType:2"] = "82";
        }

        if (classifierThreshold.HasValue)
        {
            data["WorkOrder:CategoryClassifierMinimumConfidence"] = classifierThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
        return new ApplicationConfig(configuration);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class StubCategoryClassifier : IWorkOrderCategoryClassifier
    {
        private IReadOnlyList<WorkOrderCategorySuggestion> _suggestions = [];

        public void SetSuggestions(IReadOnlyList<WorkOrderCategorySuggestion> suggestions)
            => _suggestions = suggestions;

        public IReadOnlyList<WorkOrderCategoryNode> GetCategoriesForType(int workOrderTypeId) => [];

        public Task<IReadOnlyList<WorkOrderCategorySuggestion>> ClassifyAsync(
            string description, int workOrderTypeId, CancellationToken ct = default)
            => Task.FromResult(_suggestions);
    }

    private sealed class StubFileStorage : IWorkOrderFileStorage
    {
        public Task SaveAsync(string relativePath, Stream content, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());
        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task DeleteWorkOrderFilesAsync(Guid workOrderUid, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubStatusSyncService : IWorkOrderStatusSyncService
    {
        public Task SyncStaleWorkOrdersAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
