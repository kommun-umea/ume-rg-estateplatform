using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
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

bool allowApiKeyPolicyFallback = builder.Configuration.GetValue("Authentication:EnableApiKeyPolicyFallback", builder.Environment.IsDevelopment());

AuthorizationBuilder authorizationBuilder = builder.Services.AddAuthorizationBuilder();
authorizationBuilder.AddPolicy(AuthPolicy.EstateMunicipalEmployee, policy =>
{
    policy.RequireAssertion(context =>
    {
        if (IsEmployee(context.User))
        {
            return true;
        }

        if (!allowApiKeyPolicyFallback)
        {
            return false;
        }

        HttpContext? httpContext = context.Resource switch
        {
            HttpContext ctx => ctx,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => null
        };

        if (httpContext is null)
        {
            return false;
        }

        if (!httpContext.Request.Headers.TryGetValue("X-Api-Key", out StringValues apiKeyValues))
        {
            return false;
        }

        return apiKeyValues.Any(headerValue => string.Equals(headerValue, config.ApiKey, StringComparison.Ordinal));
    });
});

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

static bool IsEmployee(ClaimsPrincipal user)
{
    Claim? idpClaim = user.Claims.FirstOrDefault(claim => string.Equals(claim.Type, "idp", StringComparison.OrdinalIgnoreCase));
    return idpClaim is not null && string.Equals(idpClaim.Value, "AzureActiveDirectory", StringComparison.OrdinalIgnoreCase);
}
