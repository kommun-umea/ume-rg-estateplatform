using System.IO.Compression;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Umea.se.Toolkit.Images.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace Umea.se.Toolkit.Images;

/// <summary>
/// Image processing service with FusionCache-based L1/L2 caching.
/// Provides automatic stampede protection, fail-safe, and eager refresh.
/// </summary>
public sealed class ImageService(IFusionCache cache, ImageServiceOptions options, ILogger<ImageService> logger)
{
    private readonly IFusionCache _cache = cache;
    private readonly ImageServiceOptions _options = options;
    private readonly ILogger<ImageService> _logger = logger;
    private readonly FusionCacheEntryOptions _cacheOptions = CreateCacheOptions(options);
    private readonly FusionCacheEntryOptions _svgCacheOptions = CreateSvgCacheOptions(options);

    /// <summary>
    /// Get cached raster image with optional resizing. Converts to WebP format.
    /// For SVG images, use <see cref="GetSvgResultAsync"/> instead.
    /// </summary>
    public async Task<byte[]> GetImageAsync(string imageId, int? maxWidth, int? maxHeight, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct = default)
    {
        ImageResult result = await GetImageResultAsync(imageId, maxWidth, maxHeight, fetchOriginal, ct);
        return result.Data;
    }

    /// <summary>
    /// Get cached SVG with GZip compression. Use when you know the content is SVG.
    /// </summary>
    public async Task<ImageResult> GetSvgResultAsync(string imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, string? svgSuffix = null, CancellationToken ct = default)
    {
        string svgKey = ImageCacheKeys.Svg(PrefixKey(imageId), svgSuffix);

        // Don't pass request cancellation token - let factory complete to populate cache
        // even if client disconnects. FusionCache timeouts protect against runaway operations.
        ImageCacheEntry entry = await _cache.GetOrSetAsync<ImageCacheEntry>(
            svgKey,
            async (ctx, token) =>
            {
                _logger.LogDebug("Fetching SVG {ImageId}", imageId);
                byte[] raw = await fetchOriginal(token);
                ValidateNotEmpty(raw, imageId);
                ImageCacheEntry result = ImageCacheEntry.SvgGzipped(GzipCompress(raw));
                ctx.Options.SetSize(result.Data.Length); // Adaptive caching: set actual size
                return result;
            },
            _svgCacheOptions,
            CancellationToken.None);

        return entry.ToImageResult();
    }

    /// <summary>
    /// Get cached raster image with optional resizing, returning full result with content type.
    /// Raster images are converted to WebP. For SVG images, use <see cref="GetSvgResultAsync"/> instead.
    /// </summary>
    public async Task<ImageResult> GetImageResultAsync(string imageId, int? maxWidth, int? maxHeight, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct = default)
    {
        // Get normalized original (cached)
        // Don't pass request cancellation token - let factory complete to populate cache
        // even if client disconnects. FusionCache timeouts protect against runaway operations.
        string prefixedId = PrefixKey(imageId);
        string originalKey = ImageCacheKeys.Original(prefixedId);
        ImageCacheEntry originalEntry = await _cache.GetOrSetAsync<ImageCacheEntry>(
            originalKey,
            async (ctx, token) =>
            {
                _logger.LogDebug("Fetching and normalizing image: {ImageId}", imageId);
                byte[] raw = await fetchOriginal(token);
                ValidateNotEmpty(raw, imageId);
                byte[] normalized = Resize(raw, _options.MaxOriginalDimension, _options.MaxOriginalDimension, _options.OriginalWebPQuality);
                _logger.LogDebug("Cached normalized original {ImageId}: {Size}KB", imageId, normalized.Length / 1024);
                ImageCacheEntry result = ImageCacheEntry.WebP(normalized);
                ctx.Options.SetSize(result.Data.Length); // Adaptive caching: set actual size
                return result;
            },
            _cacheOptions,
            CancellationToken.None);

        // Return original if no resize needed
        if (maxWidth is null && maxHeight is null)
        {
            return originalEntry.ToImageResult();
        }

        // Get thumbnail
        string thumbKey = ImageCacheKeys.Thumbnail(prefixedId, maxWidth, maxHeight);
        ImageCacheEntry thumbEntry = await _cache.GetOrSetAsync<ImageCacheEntry>(
            thumbKey,
            (ctx, token) =>
            {
                _logger.LogDebug("Creating thumbnail {Key}", thumbKey);
                byte[] thumb = Resize(originalEntry.Data, maxWidth, maxHeight, _options.WebPQuality);
                _logger.LogDebug("Cached thumbnail {Key}: {Size}KB", thumbKey, thumb.Length / 1024);
                ImageCacheEntry result = ImageCacheEntry.WebP(thumb);
                ctx.Options.SetSize(result.Data.Length); // Adaptive caching: set actual size
                return Task.FromResult(result);
            },
            _cacheOptions,
            CancellationToken.None);

        return thumbEntry.ToImageResult();
    }

    private string PrefixKey(string imageId) => $"{_options.CacheKeyPrefix}:{imageId}";

    private static FusionCacheEntryOptions CreateCacheOptions(ImageServiceOptions options) => new()
    {
        // L1 (in-memory) cache lifetime - default 24 hours
        Duration = options.MemoryCacheLifetime,
        // L2 (blob storage) cache lifetime - default 6 months
        DistributedCacheDuration = options.BlobCacheLifetime,
        // Default size estimate for L2 cache hits (100KB). Overridden with actual size in factory via adaptive caching.
        Size = 100 * 1024,
        Priority = CacheItemPriority.High,
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(365),  // Keep fail-safe data for up to 1 year
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
        FactorySoftTimeout = TimeSpan.FromSeconds(10),  // Allow time for image fetch + processing
        FactoryHardTimeout = TimeSpan.FromSeconds(30),  // Hard limit for slow networks
        EagerRefreshThreshold = 0.9f,
    };

    private static FusionCacheEntryOptions CreateSvgCacheOptions(ImageServiceOptions options) => new()
    {
        Duration = options.MemoryCacheLifetime,
        DistributedCacheDuration = options.BlobCacheLifetime,
        Size = 200 * 1024,
        Priority = CacheItemPriority.High,
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromDays(365),
        FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
        FactorySoftTimeout = TimeSpan.FromSeconds(30),   // SVG rendering is slower than image fetches
        FactoryHardTimeout = TimeSpan.FromSeconds(90),   // Pythagoras can be slow for complex blueprints
        AllowTimedOutFactoryBackgroundCompletion = true,  // Let factory finish in background so next request hits cache
        EagerRefreshThreshold = 0.9f,
    };

    #region Image Processing

    /// <summary>
    /// Resize image bytes. Validates dimensions to prevent image bombs.
    /// </summary>
    public byte[] Resize(byte[] source, int? maxWidth, int? maxHeight, int? quality = null)
    {
        using MemoryStream input = new(source, writable: false);
        return Resize(input, maxWidth, maxHeight, quality);
    }

    /// <summary>
    /// Resize from stream.
    /// </summary>
    public byte[] Resize(Stream source, int? maxWidth, int? maxHeight, int? quality = null)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        ImageInfo info = Image.Identify(source);

        if (info.Width > 20000 || info.Height > 20000)
        {
            throw new ImageTooLargeException($"Image dimensions ({info.Width}x{info.Height}) exceed maximum allowed (20000x20000)");
        }

        if (source.CanSeek)
        {
            source.Position = 0;
        }

        using Image image = Image.Load(source);
        (int targetW, int targetH) = CalculateTargetDimensions(info.Width, info.Height, maxWidth, maxHeight);

        if (targetW < info.Width || targetH < info.Height)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(targetW, targetH),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3,
            }));
        }

        using MemoryStream output = new();
        image.SaveAsWebp(output, new WebpEncoder { Quality = quality ?? _options.WebPQuality });
        return output.ToArray();
    }

    #endregion

    #region Static Helpers

    private static void ValidateNotEmpty(byte[]? data, string imageId)
    {
        if (data is null || data.Length == 0)
        {
            throw new ImageNotFoundException($"Image not found: {imageId}");
        }
    }

    private static (int width, int height) CalculateTargetDimensions(int sourceW, int sourceH, int? maxW, int? maxH)
    {
        double wRatio = maxW is > 0 ? (double)maxW.Value / sourceW : double.MaxValue;
        double hRatio = maxH is > 0 ? (double)maxH.Value / sourceH : double.MaxValue;
        double ratio = Math.Min(Math.Min(wRatio, hRatio), 1.0);

        return ((int)Math.Round(sourceW * ratio), (int)Math.Round(sourceH * ratio));
    }

    private static byte[] GzipCompress(byte[] data)
    {
        using MemoryStream output = new();
        using (GZipStream gzip = new(output, CompressionLevel.Optimal))
        {
            gzip.Write(data);
        }
        return output.ToArray();
    }

    #endregion
}
