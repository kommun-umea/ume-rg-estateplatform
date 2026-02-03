namespace Umea.se.Toolkit.Images;

public class ImageServiceOptions
{
    /// <summary>
    /// Required prefix for all cache keys, identifying the data source.
    /// Example: "pythagoras" results in paths like cache/pythagoras/images/123/original.webp
    /// </summary>
    public required string CacheKeyPrefix { get; set; }

    /// <summary>
    /// Maximum dimension for normalized originals. Default: 2560px
    /// Covers 1440p displays. Prevents large image bombs while supporting hero images.
    /// </summary>
    public int MaxOriginalDimension { get; set; } = 2560;

    /// <summary>
    /// WebP quality for thumbnails sent to users (1-100). Default: 80
    /// </summary>
    public int WebPQuality { get; set; } = 80;

    /// <summary>
    /// WebP quality for normalized originals (internal cache). Default: 95
    /// Higher quality prevents generation loss when creating thumbnails.
    /// </summary>
    public int OriginalWebPQuality { get; set; } = 95;

    /// <summary>
    /// L1 (in-memory) cache lifetime. Default: 24 hours
    /// </summary>
    public TimeSpan MemoryCacheLifetime { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// L2 (blob storage) cache lifetime. Default: 6 months (180 days)
    /// </summary>
    public TimeSpan BlobCacheLifetime { get; set; } = TimeSpan.FromDays(180);
}
