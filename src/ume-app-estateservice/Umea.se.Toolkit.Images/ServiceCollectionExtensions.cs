using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umea.se.Toolkit.Images.Caching;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.ProtoBufNet;

namespace Umea.se.Toolkit.Images;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ImageService with FusionCache-based L1/L2 caching.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="cacheKeyPrefix">Required prefix for all cache keys (e.g., "estateservice").</param>
    /// <param name="configureOptions">Configure additional image service options.</param>
    /// <param name="configureBlobCache">Configure blob storage for L2 cache. If null or not configured, uses memory-only caching.</param>
    public static IServiceCollection AddImageService(
        this IServiceCollection services,
        string cacheKeyPrefix,
        Action<ImageServiceOptions>? configureOptions = null,
        Action<BlobCacheOptions>? configureBlobCache = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKeyPrefix);

        // Register ImageServiceOptions
        ImageServiceOptions imageOptions = new() { CacheKeyPrefix = cacheKeyPrefix };
        configureOptions?.Invoke(imageOptions);
        services.AddSingleton(imageOptions);

        // Register BlobCacheOptions
        BlobCacheOptions blobOptions = new();
        configureBlobCache?.Invoke(blobOptions);
        services.AddSingleton(blobOptions);

        // L2 Distributed Cache (Blob Storage or Memory fallback)
        services.AddSingleton<IDistributedCache>(sp =>
        {
            BlobCacheOptions options = sp.GetRequiredService<BlobCacheOptions>();
            ILogger<BlobDistributedCache> logger = sp.GetRequiredService<ILogger<BlobDistributedCache>>();

            logger.LogWarning(
                "BlobCacheOptions - UseConnectionString: {HasConnStr}, ServiceUri: {ServiceUri}, ContainerName: {Container}, IsConfigured: {IsConfigured}",
                !string.IsNullOrWhiteSpace(options.ConnectionString),
                options.ServiceUri,
                options.ContainerName,
                options.IsConfigured);

            if (!options.IsConfigured)
            {
                logger.LogWarning("Blob cache not configured, using memory-only distributed cache");
                return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            }

            try
            {
                BlobContainerClient container = CreateBlobContainerClient(options, logger);
                return new BlobDistributedCache(container, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize blob cache, falling back to memory-only");
                return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            }
        });

        // FusionCache with L1 (Memory, 500MB limit) + L2 (Blob Storage) + Protobuf serialization
        // Size is set via adaptive caching in factory, with a default fallback for L2 cache hits
        services.AddFusionCache()
            .WithMemoryCache(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 500 * 1024 * 1024, // 500 MB
                CompactionPercentage = 0.25
            }))
            .WithSerializer(new FusionCacheProtoBufNetSerializer())
            .WithDistributedCache(sp => sp.GetRequiredService<IDistributedCache>())
            .WithPostSetup((sp, cache) =>
            {
#if DEBUG
                ILogger cacheLogger = sp.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("FusionCache.ImageService");

                cache.Events.Miss += (_, e) => cacheLogger.LogDebug("Cache MISS: {Key}", e.Key);
                cache.Events.Set += (_, e) => cacheLogger.LogDebug("Cache SET: {Key}", e.Key);
                cache.Events.FailSafeActivate += (_, e) => cacheLogger.LogWarning("Cache FAILSAFE: {Key}", e.Key);
                cache.Events.Memory.Hit += (_, e) => cacheLogger.LogDebug(
                    "Cache HIT [L1-Memory{Stale}]: {Key}",
                    e.IsStale ? "-STALE" : "",
                    e.Key);
                cache.Events.Distributed.Hit += (_, e) => cacheLogger.LogDebug(
                    "Cache HIT [L2-Blob{Stale}]: {Key}",
                    e.IsStale ? "-STALE" : "",
                    e.Key);
#endif
            });

        // Register ImageService
        services.AddSingleton<ImageService>();

        return services;
    }

    private static BlobContainerClient CreateBlobContainerClient(BlobCacheOptions options, ILogger logger)
    {
        BlobClientOptions clientOptions = new()
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                MaxRetries = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                MaxDelay = TimeSpan.FromSeconds(10)
            }
        };

        BlobServiceClient serviceClient;

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            logger.LogInformation("Using connection string for blob storage authentication");
            serviceClient = new BlobServiceClient(options.ConnectionString, clientOptions);
        }
        else if (options.ServiceUri is not null)
        {
            logger.LogInformation("Using DefaultAzureCredential for blob storage authentication: {Uri}", options.ServiceUri);
            serviceClient = new BlobServiceClient(options.ServiceUri, new DefaultAzureCredential(), clientOptions);
        }
        else
        {
            throw new InvalidOperationException("BlobCacheOptions requires either ConnectionString or ServiceUri to be configured.");
        }

        BlobContainerClient container = serviceClient.GetBlobContainerClient(options.ContainerName);

        if (options.CreateContainerIfNotExists)
        {
            container.CreateIfNotExists();
            logger.LogInformation("Ensured blob cache container {Container} exists", options.ContainerName);
        }

        logger.LogInformation("Blob distributed cache initialized: {Uri}", container.Uri);
        return container;
    }
}
