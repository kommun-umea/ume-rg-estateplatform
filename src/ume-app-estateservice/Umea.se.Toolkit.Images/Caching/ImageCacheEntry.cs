using ProtoBuf;

namespace Umea.se.Toolkit.Images.Caching;

/// <summary>
/// Wrapper for cached image data used with FusionCache.
/// Uses Protobuf for efficient binary serialization.
/// </summary>
[ProtoContract]
public sealed class ImageCacheEntry
{
    [ProtoMember(1)]
    public byte[] Data { get; init; } = [];

    [ProtoMember(2)]
    public string ContentType { get; init; } = "image/webp";

    [ProtoMember(3)]
    public bool IsGzipped { get; init; }

    /// <summary>
    /// Converts to ImageResult for API response.
    /// </summary>
    public ImageResult ToImageResult() => new(Data, ContentType, IsGzipped);

    /// <summary>
    /// Creates cache entry from ImageResult.
    /// </summary>
    public static ImageCacheEntry FromImageResult(ImageResult result)
        => new()
        {
            Data = result.Data,
            ContentType = result.ContentType,
            IsGzipped = result.IsGzipped
        };

    /// <summary>
    /// Creates WebP cache entry.
    /// </summary>
    public static ImageCacheEntry WebP(byte[] data)
        => new() { Data = data, ContentType = "image/webp", IsGzipped = false };

    /// <summary>
    /// Creates gzipped SVG cache entry.
    /// </summary>
    public static ImageCacheEntry SvgGzipped(byte[] data)
        => new() { Data = data, ContentType = "image/svg+xml", IsGzipped = true };
}
