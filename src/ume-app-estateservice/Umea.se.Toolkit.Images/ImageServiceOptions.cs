namespace Umea.se.Toolkit.Images;

public class ImageServiceOptions
{
    /// <summary>
    /// Cache size limit in MB. Default: 500MB
    /// </summary>
    public int CacheSizeMb { get; init; } = 500;

    /// <summary>
    /// Maximum dimension for normalized originals. Default: 2560px
    /// Covers 1440p displays. Prevents large image bombs while supporting hero images.
    /// </summary>
    public int MaxOriginalDimension { get; init; } = 2560;

    /// <summary>
    /// WebP quality for thumbnails sent to users (1-100). Default: 80
    /// </summary>
    public int WebPQuality { get; init; } = 80;

    /// <summary>
    /// WebP quality for normalized originals (internal cache). Default: 95
    /// Higher quality prevents generation loss when creating thumbnails.
    /// </summary>
    public int OriginalWebPQuality { get; init; } = 95;

    /// <summary>
    /// Cache lifetime. Default: 24h
    /// </summary>
    public TimeSpan CacheLifetime { get; init; } = TimeSpan.FromHours(24);
}
