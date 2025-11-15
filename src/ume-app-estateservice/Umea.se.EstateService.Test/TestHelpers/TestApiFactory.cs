using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess;
using Microsoft.AspNetCore.TestHost;
using Umea.se.TestToolkit.TestInfrastructure;
using Umea.se.EstateService.ServiceAccess.Data;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Application factory that replaces external dependencies with fakes for integration testing.
/// </summary>
public sealed class TestApiFactory : WebAppFactoryBase<Program, HttpClientNames>
{
    public const string ApiKey = "test-api-key";
    public const string PythagorasBaseUrl = "https://localhost/";

    private readonly FakePythagorasClient _fakeClient = new();
    public FakePythagorasClient FakeClient => _fakeClient;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["Api:Key"] = ApiKey,
                ["suppressKeyVaultConfigs"] = "true",
                ["KeyVaultUrl"] = "https://localhost/",
                ["Pythagoras-Base-Url"] = PythagorasBaseUrl,
                ["Pythagoras-Api-Key"] = "pythagoras-test-key",
                // Disable automatic data store refresh during tests to avoid interference
                ["EstateData:RefreshIntervalHours"] = "0"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPythagorasClient>();
            services.RemoveAll<IPythagorasHandler>();
            services.RemoveAll<IDataStore>();

            services.AddSingleton<FakePythagorasClient>(_ => _fakeClient);
            services.AddSingleton<IPythagorasClient>(_ => _fakeClient);
            services.AddSingleton<IPythagorasHandler>(_ => new PythagorasHandler(_fakeClient));

            // Use a test-friendly in-memory data store
            services.AddSingleton<IDataStore>(_ => new InMemoryDataStore());
        });
    }
}
