using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Application factory that replaces external dependencies with fakes for integration testing.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";
    public const string PythagorasBaseUrl = "https://localhost/";

    public FakePythagorasClient FakeClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["Api:Key"] = ApiKey,
                ["suppressKeyVaultConfigs"] = "true",
                ["KeyVaultUrl"] = "https://localhost/",
                ["Pythagoras-Base-Url"] = PythagorasBaseUrl,
                ["Pythagoras-Api-Key"] = "pythagoras-test-key"
            };

            configurationBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPythagorasClient>();
            services.RemoveAll<IPythagorasHandler>();

            services.AddSingleton<IPythagorasClient>(FakeClient);
            services.AddSingleton<IPythagorasHandler>(_ => new PythagorasHandler(FakeClient));
        });
    }
}
