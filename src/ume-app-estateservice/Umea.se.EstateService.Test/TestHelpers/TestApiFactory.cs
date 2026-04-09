using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Handlers.Images;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.Shared.Data;
using Umea.se.TestToolkit.TestInfrastructure;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Application factory that replaces external dependencies with fakes for integration testing.
/// </summary>
public sealed class TestApiFactory : WebAppFactoryBase<Program, HttpClientNames>
{
    // Keep the SQLite connection open so the in-memory database persists across DbContext instances
    private readonly SqliteConnection _dbConnection = new("DataSource=:memory:");

    public const string ApiKey = "test-api-key-for-integration-tests";
    public const string PythagorasBaseUrl = "https://localhost/";

    private readonly FakePythagorasClient _fakeClient = new();
    private readonly StubBuildingImageService _stubImageService = new();

    public FakePythagorasClient FakeClient => _fakeClient;
    public StubBuildingImageService StubImageService => _stubImageService;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["ASPNETCORE_ENVIRONMENT"] = "IntegrationTest",
                ["ConnectionStrings:EstateService"] = "DataSource=:memory:",
                ["Api:Keys:Default"] = ApiKey,
                ["suppressKeyVaultConfigs"] = "true",
                ["KeyVaultUrl"] = "https://localhost/",
                ["Pythagoras-Base-Url"] = PythagorasBaseUrl,
                ["Pythagoras-Api-Key"] = "pythagoras-test-key",
                // Enable all features for integration tests
                ["Features"] = "EstateService,ContactPersons,Documents,ErrorReport",
                // Disable scheduled refresh during tests (no CronSchedule = manual only)
                // Work order submission config for tests
                ["WorkOrder:FileStorage"] = "./test-workorder-files",
                ["WorkOrder:ProcessingIntervalSeconds"] = "9999",
                ["WorkOrder:MaxRetries"] = "3",
                ["WorkOrder:RetryBaseDelaySeconds"] = "1",
                ["WorkOrder:StatusCheckIntervalMinutes"] = "60"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPythagorasClient>();
            services.RemoveAll<IEstateDataQueryHandler>();
            services.RemoveAll<IBuildingImageService>();
            services.RemoveAll<IDataStore>();
            services.RemoveAll<IDataStorePersistence>();
            services.RemoveAll<InMemoryDataStore>();

            services.AddSingleton<FakePythagorasClient>(_ => _fakeClient);
            services.AddSingleton<IPythagorasClient>(_ => _fakeClient);
            services.AddSingleton<StubBuildingImageService>(_ => _stubImageService);
            services.AddSingleton<IBuildingImageService>(_ => _stubImageService);

            services.AddSingleton<InMemoryDataStore>();
            services.AddSingleton<IDataStore>(sp => sp.GetRequiredService<InMemoryDataStore>());
            services.AddSingleton<IEstateDataQueryHandler>(sp => new EstateDataQueryHandler(sp.GetRequiredService<IDataStore>()));

            // Add no-op persistence for tests
            services.AddSingleton<IDataStorePersistence, NullDataStorePersistence>();

            // Add in-memory SQLite database for work order tests
            _dbConnection.Open();
            services.RemoveAll<IDbContextFactory<EstateDbContext>>();
            services.AddDbContextFactory<EstateDbContext>(options =>
                options.UseSqlite(_dbConnection));

            // Register repositories (shared with production via AddRepositories)
            services.AddRepositories();

            // Ensure the database schema is created
            using EstateDbContext tempContext = new(
                new DbContextOptionsBuilder<EstateDbContext>().UseSqlite(_dbConnection).Options);
            tempContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _dbConnection.Dispose();
        }
    }
}
