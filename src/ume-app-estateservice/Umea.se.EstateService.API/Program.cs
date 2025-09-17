using Azure.Monitor.OpenTelemetry.AspNetCore;
using Umea.se.EstateService.API;
using Umea.se.EstateService.Logic;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.Toolkit.EntryPoints;
using Umea.se.Toolkit.Filters;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ApplicationConfig config = new(builder.Configuration);

builder.Services.ConnectToKeyVault(config.KeyVaultUrl);

if (!config.SuppressKeyVaultConfigs)
{
    config.LoadKeyVaultSecrets();
    builder.Logging.UseDefaultLoggers(config);
}

builder.Services
    .AddApplicationConfig(config)
    .AddApiDependencies()
    .AddLogicDependencies()
    .AddServiceAccessDependencies()
    .AddSharedDependencies()
    ;

builder.Services.AddDefaultHttpClient(HttpClientNames.Pythagoras, options =>
{
    options.BaseAddress = config.PythagorasBaseUrl;
    options.XApiKey = config.PythagorasApiKey;
});

builder.Services.AddAuthentication(options =>
{
    //API Key authentication?
});

// Check with the team if we should use OpenTelemetry
builder.Services
    .AddOpenTelemetry()
    .UseAzureMonitor(options => { options.ConnectionString = config.ApplicationInsightsConnectionString; });

// Swagger
builder.Services.AddDefaultSwagger(config);

builder.Services.AddAllowedOriginsCorsPolicy(config.AllowedOrigins);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<HttpResponseExceptionFilter>();
});

builder.Services.AddDefaultSwagger(config);

WebApplication app = builder.Build();

app.UseDefaultSwagger(config);
app.UseHttpsRedirection();
app.UseAllowedOriginsCorsPolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
