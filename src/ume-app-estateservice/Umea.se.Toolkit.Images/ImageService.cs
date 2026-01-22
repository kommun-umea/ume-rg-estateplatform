using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Umea.se.Toolkit.Images;

public sealed class ImageService(ImageServiceOptions options, IMemoryCache cache, ILogger<ImageService> logger)
{
    private readonly IMemoryCache _cache = cache;
    private readonly ConcurrentDictionary<string, Task<byte[]>> _pendingTasks = new();
    private readonly ImageServiceOptions _options = options;
    private readonly ILogger<ImageService> _logger = logger;

    /// <summary>
    /// Get cached image with optional resizing.
    /// Two-tier cache: normalized original (max 2560px) + thumbnails.
    /// Avoids re-downloading large originals for each thumbnail size.
    /// </summary>
    public async Task<byte[]> GetImageAsync(string imageId, int? maxWidth, int? maxHeight, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct = default)
    {
        byte[] normalizedOriginal = await GetOrCreateNormalizedOriginalAsync(imageId, fetchOriginal, ct);

        if (maxWidth is null && maxHeight is null)
        {
            return normalizedOriginal;
        }

        return await GetOrCreateThumbnailAsync(imageId, maxWidth, maxHeight, normalizedOriginal, ct);
    }

    private async Task<byte[]> GetOrCreateNormalizedOriginalAsync(string imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct)
    {
        string key = $"img:{imageId}:original";

        if (_cache.TryGetValue(key, out byte[]? cached))
        {
            return cached!;
        }

        Task<byte[]> task = _pendingTasks.GetOrAdd(key, _ =>
            FetchAndCacheOriginalAsync(key, imageId, fetchOriginal, ct));

        return await task.WaitAsync(ct);
    }

    private async Task<byte[]> FetchAndCacheOriginalAsync(string key, string imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Fetching original for {ImageId}", imageId);

            byte[] raw = await fetchOriginal(ct);

            if (raw is null || raw.Length == 0)
            {
                throw new ImageNotFoundException($"Image not found: {imageId}");
            }

            byte[] normalized = Resize(raw, _options.MaxOriginalDimension, _options.MaxOriginalDimension, _options.OriginalWebPQuality);

            _cache.Set(key, normalized, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_options.CacheLifetime)
                .SetPriority(CacheItemPriority.High)
                .SetSize(normalized.Length));

            _logger.LogDebug("Cached {ImageId}: {Size}KB", imageId, normalized.Length / 1024);

            return normalized;
        }
        finally
        {
            _pendingTasks.TryRemove(key, out _);
        }
    }

    private async Task<byte[]> GetOrCreateThumbnailAsync(string imageId, int? maxWidth, int? maxHeight, byte[] normalizedOriginal, CancellationToken ct)
    {
        string key = $"img:{imageId}:{maxWidth ?? 0}x{maxHeight ?? 0}";

        // Fast path - already cached
        if (_cache.TryGetValue(key, out byte[]? cached))
        {
            return cached!;
        }

        // Coalesce concurrent requests
        Task<byte[]> task = _pendingTasks.GetOrAdd(key, _ =>
            CreateAndCacheThumbnailAsync(key, normalizedOriginal, maxWidth, maxHeight));

        // Allow per-request cancellation of the wait (not the work)
        return await task.WaitAsync(ct);
    }

    private async Task<byte[]> CreateAndCacheThumbnailAsync(string key, byte[] normalizedOriginal, int? maxWidth, int? maxHeight)
    {
        try
        {
            byte[] thumbnail = await Task.Run(() => Resize(normalizedOriginal, maxWidth, maxHeight, _options.WebPQuality));

            _cache.Set(key, thumbnail, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_options.CacheLifetime)
                .SetSize(thumbnail.Length));

            _logger.LogDebug("Cached thumbnail {Key}: {Size}KB", key, thumbnail.Length / 1024);

            return thumbnail;
        }
        finally
        {
            _pendingTasks.TryRemove(key, out _);
        }
    }

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

        // Prevent image bomb attacks - reject images with extreme dimensions
        if (info.Width > 20000 || info.Height > 20000)
        {
            throw new ImageTooLargeException($"Image dimensions ({info.Width}x{info.Height}) exceed maximum allowed size (20000x20000)");
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

    private static (int width, int height) CalculateTargetDimensions(int sourceW, int sourceH, int? maxW, int? maxH)
    {
        double wRatio = maxW is > 0 ? (double)maxW.Value / sourceW : double.MaxValue;
        double hRatio = maxH is > 0 ? (double)maxH.Value / sourceH : double.MaxValue;
        double ratio = Math.Min(Math.Min(wRatio, hRatio), 1.0);

        return ((int)Math.Round(sourceW * ratio), (int)Math.Round(sourceH * ratio));
    }

}
