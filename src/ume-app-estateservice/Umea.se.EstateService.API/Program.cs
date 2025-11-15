using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Umea.se.EstateService.API;
using Umea.se.EstateService.Logic;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.Toolkit.EntryPoints;
using Umea.se.Toolkit.Filters;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ApplicationConfig config = new(builder.Configuration);

if (!builder.Environment.IsEnvironment("IntegrationTest"))
{
    builder.Services.ConnectToKeyVault(config.KeyVaultUrl);

    if (!config.SuppressKeyVaultConfigs)
    {
        config.LoadKeyVaultSecrets();
    }

    builder.Logging.UseDefaultLoggers(config);
}
else
{
    builder.Logging.ClearProviders();
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
    options.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
});

builder.Services
    .AddAuthorization()
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = config.Authentication.TokenServiceUrl;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config.Authentication.TokenServiceUrl,
            ValidateAudience = true,
            ValidAudience = config.Authentication.Audience,
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero,
        };
    });

// Swagger
if (!builder.Environment.IsEnvironment("IntegrationTest"))
{
    builder.Services.AddDefaultSwagger(config);
    builder.Services.ConfigureSwaggerGen(options => options.CustomSchemaIds(x => x.FullName));
}

builder.Services.AddAllowedOriginsCorsPolicy(config.AllowedOrigins);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<HttpResponseExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

WebApplication app = builder.Build();

if (!app.Environment.IsEnvironment("IntegrationTest"))
{
    app.UseDefaultSwagger(config);
}
app.UseHttpsRedirection();
app.UseAllowedOriginsCorsPolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
