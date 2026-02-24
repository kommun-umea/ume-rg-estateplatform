using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Umea.se.EstateService.API;
using Umea.se.EstateService.API.Infrastructure;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.Logic;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.Shared;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.Toolkit.EntryPoints;
using Umea.se.Toolkit.Filters;
using Umea.se.Toolkit.Images;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ApplicationConfig config = new(builder.Configuration, typeof(Program).Assembly);

if (!builder.Environment.IsEnvironment("IntegrationTest"))
{
    builder.Logging.UseDefaultLoggers(config);
}
else
{
    builder.Logging.ClearProviders();
}

// ImageService must be registered before AddLogicDependencies (BuildingImageService depends on it)
builder.Services.AddImageService(
    cacheKeyPrefix: "estateservice",
    configureOptions: options =>
    {
        options.MemoryCacheLifetime = TimeSpan.FromHours(config.ImageCache.MemoryCacheLifetimeHours);
        options.BlobCacheLifetime = TimeSpan.FromDays(config.ImageCache.BlobCacheLifetimeDays);
    },
    configureBlobCache: blobCache =>
    {
        blobCache.ConnectionString = config.ImageCache.BlobConnectionString;
        blobCache.ServiceUri = config.ImageCache.BlobServiceUrl is not null
            ? new Uri(config.ImageCache.BlobServiceUrl)
            : null;
        blobCache.ContainerName = config.ImageCache.BlobContainerName ?? "imagecache";
    });

builder.Services
    .AddApplicationConfig(config)
    .AddApiDependencies()
    .AddLogicDependencies()
    .AddDataStorePersistence(builder.Configuration.GetConnectionString("EstateService"), config.DataSync)
    .AddServiceAccessDependencies()
    .AddSharedDependencies()
;

builder.Services.AddHttpClient(HttpClientNames.Pythagoras, client =>
{
    client.BaseAddress = new Uri(config.PythagorasBaseUrl);
    client.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
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
    builder.Services.ConfigureSwaggerGen(options =>
    {
        options.CustomSchemaIds(x => x.FullName);
        // Fix: FromQuery model properties incorrectly marked as required
        // See: https://github.com/dotnet/aspnetcore/issues/52881
        options.OperationFilter<NullableQueryParametersOperationFilter>();
    });
}

builder.Services.AddAllowedOriginsCorsPolicy(config.AllowedOrigins);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ["application/json", "text/json", "application/problem+json"];
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<HttpResponseExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

WebApplication app = builder.Build();

app.UseResponseCompression();

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
