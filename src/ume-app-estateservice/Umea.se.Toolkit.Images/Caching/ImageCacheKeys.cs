namespace Umea.se.Toolkit.Images.Caching;

/// <summary>
/// Generates cache keys that double as human-readable blob paths.
/// Format: cache/{prefix}/{collection}/{id}/{variant}.{extension}
/// The prefix is configured via ImageServiceOptions.CacheKeyPrefix.
/// </summary>
public static class ImageCacheKeys
{
    /// <summary>
    /// Path for cached gzipped SVG (used for floor blueprints).
    /// Example: cache/estateservice/floors/123/blueprint.svg.gz
    /// </summary>
    public static string Svg(string imageId) => BlobPathBuilder.ForImage(imageId, "svg");

    /// <summary>
    /// Path for normalized original (max 2560px, high-quality WebP).
    /// Example: cache/estateservice/images/12345/original.webp
    /// </summary>
    public static string Original(string imageId) => BlobPathBuilder.ForImage(imageId, "original");

    /// <summary>
    /// Path for resized thumbnail.
    /// Example: cache/estateservice/images/12345/200x300.webp
    /// </summary>
    public static string Thumbnail(string imageId, int? width, int? height)
        => BlobPathBuilder.ForImage(imageId, $"{width ?? 0}x{height ?? 0}");
}
