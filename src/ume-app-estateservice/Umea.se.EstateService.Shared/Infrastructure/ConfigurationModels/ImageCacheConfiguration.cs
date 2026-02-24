namespace Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

public class ImageCacheConfiguration
{
    /// <summary>
    /// L1 (in-memory) image cache lifetime in hours. Default: 24 hours.
    /// </summary>
    public int MemoryCacheLifetimeHours { get; set; } = 24;

    /// <summary>
    /// L2 (blob storage) image cache lifetime in days. Default: 180 days (6 months).
    /// </summary>
    public int BlobCacheLifetimeDays { get; set; } = 180;

    /// <summary>
    /// Blob storage connection string for L2 image cache.
    /// Used for local development (Azurite) or when managed identity is not available.
    /// </summary>
    public string? BlobConnectionString { get; set; }

    /// <summary>
    /// Blob service URL for managed identity authentication.
    /// Example: "https://umeestatestgdev.blob.core.windows.net"
    /// Used when BlobConnectionString is not set.
    /// </summary>
    public string? BlobServiceUrl { get; set; }

    /// <summary>
    /// Blob container name for L2 image cache.
    /// Example: "ume-stc-imagecache-dev"
    /// </summary>
    public string? BlobContainerName { get; set; }
}
