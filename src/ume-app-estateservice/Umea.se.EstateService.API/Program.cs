using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Umea.se.EstateService.API;
using Umea.se.EstateService.Logic;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.Toolkit.Auth;
using Umea.se.Toolkit.Auth.Authorization;
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

builder.Services.AddEmployeeOrApiKeyAuthorization(config);
config.ValidateApiKeys();

AuthenticationBuilder authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.Authority = config.TokenServiceAddress;
        List<string> validAudiences = new();
        if (!string.IsNullOrWhiteSpace(config.ApiName))
        {
            validAudiences.Add(config.ApiName);
        }

        if (!string.IsNullOrWhiteSpace(config.EmployeeClaimValue))
        {
            validAudiences.Add(config.EmployeeClaimValue);
        }

        if (validAudiences.Count > 0)
        {
            options.TokenValidationParameters.ValidAudiences = validAudiences;
        }
    });

authenticationBuilder.AddApiKeyAuthentication();

builder.Logging.UseDefaultLoggers(config);

// Swagger
builder.Services.AddDefaultSwagger(config);
builder.Services.ConfigureSwaggerGen(options =>
{
    options.CustomSchemaIds(x => x.FullName);

    if (!options.SwaggerGeneratorOptions.SecuritySchemes.ContainsKey("Bearer"))
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
        });
    }

    if (!options.SwaggerGeneratorOptions.SecuritySchemes.ContainsKey("ApiKey"))
    {
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key sent in the X-Api-Key header.",
            Name = "X-Api-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
        });
    }
});

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
