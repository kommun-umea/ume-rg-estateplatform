using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Umea.se.EstateService.API;
using Umea.se.EstateService.API.Infrastructure;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.Logic;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.ServiceAccess;
using Umea.se.EstateService.ServiceAccess.FileStorage;
using Umea.se.EstateService.Shared;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;
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

builder.Services.AddProblemDetails();

builder.Services.AddFeatureFlags();

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
    .AddDataStorePersistence(config.DatabaseConnectionString)
    .AddServiceAccessDependencies()
    .AddSharedDependencies()
;

builder.Services.AddSingleton<IWorkOrderFileStorage>(sp =>
{
    ApplicationConfig appConfig = sp.GetRequiredService<ApplicationConfig>();
    WorkOrderConfiguration woConfig = appConfig.WorkOrderProcessing;
    return woConfig.ResolvedStorageType switch
    {
        FileStorageType.BlobUrl => CreateBlobStorage(
            new Azure.Storage.Blobs.BlobServiceClient(new Uri(woConfig.FileStorage), new DefaultAzureCredential()),
            woConfig.FileStorageContainer),
        FileStorageType.BlobConnectionString => CreateBlobStorage(
            new Azure.Storage.Blobs.BlobServiceClient(woConfig.FileStorage),
            woConfig.FileStorageContainer),
        _ => new LocalWorkOrderFileStorage(appConfig)
    };

    static BlobWorkOrderFileStorage CreateBlobStorage(Azure.Storage.Blobs.BlobServiceClient serviceClient, string containerName)
    {
        BlobContainerClient container = serviceClient.GetBlobContainerClient(containerName);
        if (!container.Exists())
        {
            container.Create();
        }

        return new BlobWorkOrderFileStorage(container);
    }
});

builder.Services.AddSingleton(sp =>
{
    ApplicationConfig appConfig = sp.GetRequiredService<ApplicationConfig>();
    return new AzureOpenAIClient(new Uri(appConfig.OpenAI.Endpoint), new DefaultAzureCredential())
        .GetChatClient(appConfig.OpenAI.Model)
        .AsIChatClient();
});

builder.Services.AddScoped<IWorkOrderCategoryClassifier>(sp =>
{
    ApplicationConfig appConfig = sp.GetRequiredService<ApplicationConfig>();
    if (appConfig.OpenAI.Enabled)
    {
        return ActivatorUtilities.CreateInstance<WorkOrderCategoryClassifier>(sp);
    }
    return new NullWorkOrderCategoryClassifier();
});

builder.Services.AddHttpClient(HttpClientNames.Pythagoras, client =>
{
    client.BaseAddress = new Uri(config.PythagorasBaseUrl);
    client.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
});

// Gallery image fetches have their own tighter budget so they fit inside the raster image
// FusionCache FactoryHardTimeout (45s). This prevents slow image fetches from being torn down
// mid-retry with TaskCanceled/Socket 995 noise.
// See Umea.se.Toolkit.Images.ImageService CreateCacheOptions for the raster cache timings.
builder.Services.AddHttpClient(HttpClientNames.PythagorasImages, client =>
{
    client.BaseAddress = new Uri(config.PythagorasBaseUrl);
    client.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(40);
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
});

// Blueprint rendering in Pythagoras is significantly slower than other endpoints.
// Dedicated client with higher timeouts to match the SVG FusionCache budget (45s/120s).
builder.Services.AddHttpClient(HttpClientNames.PythagorasBlueprints, client =>
{
    client.BaseAddress = new Uri(config.PythagorasBaseUrl);
    client.DefaultRequestHeaders.Add("api_key", config.PythagorasApiKey);
})
.AddStandardResilienceHandler(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(45);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.UseJitter = true;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
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
            ValidateLifetime = !builder.Environment.IsDevelopment(),
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
    options.Filters.Add<EstateServiceExceptionFilter>();
    options.Filters.Add<HttpResponseExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

WebApplication app = builder.Build();

await app.Services.EnsureDatabaseCreatedAsync();

app.UseResponseCompression();

if (!app.Environment.IsEnvironment("IntegrationTest"))
{
    app.UseDefaultSwagger(config);
    app.UseHttpsRedirection();
}
app.UseAllowedOriginsCorsPolicy();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<EstateServiceFeatureGateMiddleware>();
app.MapControllers();

app.Run();
