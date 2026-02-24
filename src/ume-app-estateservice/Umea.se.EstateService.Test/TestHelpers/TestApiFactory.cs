using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
                ["Api:Keys:Default"] = ApiKey,
                ["suppressKeyVaultConfigs"] = "true",
                ["KeyVaultUrl"] = "https://localhost/",
                ["Pythagoras-Base-Url"] = PythagorasBaseUrl,
                ["Pythagoras-Api-Key"] = "pythagoras-test-key",
                // Disable automatic data store refresh during tests to avoid interference
                ["DataSync:RefreshIntervalHours"] = "0"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPythagorasClient>();
            services.RemoveAll<IEstateDataQueryHandler>();
            services.RemoveAll<IBuildingImageService>();
            services.RemoveAll<IDataStore>();
            services.RemoveAll<BuildingImageIdCache>();
            services.RemoveAll<IDataStorePersistence>();
            services.RemoveAll<InMemoryDataStore>();

            services.AddSingleton<FakePythagorasClient>(_ => _fakeClient);
            services.AddSingleton<IPythagorasClient>(_ => _fakeClient);
            services.AddSingleton<StubBuildingImageService>(_ => _stubImageService);
            services.AddSingleton<IBuildingImageService>(_ => _stubImageService);

            services.AddSingleton<InMemoryDataStore>();
            services.AddSingleton<IDataStore>(sp => sp.GetRequiredService<InMemoryDataStore>());
            services.AddSingleton<IEstateDataQueryHandler>(sp => new EstateDataQueryHandler(sp.GetRequiredService<IDataStore>()));
            services.AddSingleton<BuildingImageIdCache>();

            // Add no-op persistence for tests
            services.AddSingleton<IDataStorePersistence, NullDataStorePersistence>();
        });
    }
}
