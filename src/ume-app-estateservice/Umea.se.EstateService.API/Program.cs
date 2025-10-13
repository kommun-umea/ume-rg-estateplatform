using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Umea.se.EstateService.API;
using Umea.se.EstateService.API.Authorization;
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
    options.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
});

builder.Services
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
        options.Audience = config.ApiName;
        options.TokenValidationParameters.ValidAudience = config.ApiName;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.Employee, static policy =>
    {
        policy.Requirements.Add(new EmployeeRequirement());
    });
});

builder.Services.AddSingleton<IAuthorizationHandler, EmployeeAuthorizationHandler>();

builder.Logging.UseDefaultLoggers(config);

// Swagger
builder.Services.AddDefaultSwagger(config);
builder.Services.ConfigureSwaggerGen(options => options.CustomSchemaIds(x => x.FullName));

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

app.UseDefaultSwagger(config);
app.UseHttpsRedirection();
app.UseAllowedOriginsCorsPolicy();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
