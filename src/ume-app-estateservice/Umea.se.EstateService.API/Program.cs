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
    //options.XApiKey = config.PythagorasApiKey;
    // The below works when improvement/14157 is merged and released
    options.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
});

builder.Services.AddAuthentication(options =>
{

});

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

WebApplication app = builder.Build();

app.UseDefaultSwagger(config);
app.UseHttpsRedirection();
app.UseAllowedOriginsCorsPolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
