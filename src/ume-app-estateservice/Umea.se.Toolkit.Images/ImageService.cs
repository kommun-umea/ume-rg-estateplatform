using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Umea.se.Toolkit.Images;

public sealed class ImageService(ImageServiceOptions options, IMemoryCache cache, ILogger<ImageService> logger)
{
    private readonly ConcurrentDictionary<string, Task<byte[]>> _pendingTasks = new();

    /// <summary>
    /// Get cached image with optional resizing.
    /// </summary>
    public async Task<byte[]> GetImageAsync(string imageId, int? maxWidth, int? maxHeight, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct = default)
    {
        ImageResult result = await GetImageResultAsync(imageId, maxWidth, maxHeight, fetchOriginal, ct);
        return result.Data;
    }

    /// <summary>
    /// Get cached SVG with GZip compression. Use when you know the content is SVG.
    /// </summary>
    public async Task<ImageResult> GetSvgResultAsync(string imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct = default)
    {
        string svgKey = $"img:{imageId}:svg";

        if (cache.TryGetValue(svgKey, out byte[]? cached))
        {
            return ImageResult.SvgGzipped(cached!);
        }

        byte[] raw = await fetchOriginal(ct);
        ValidateNotEmpty(raw, imageId);

        return CacheAndReturnSvg(svgKey, imageId, raw);
    }

    /// <summary>
    /// Get cached image with optional resizing, returning full result with content type.
    /// SVGs are GZip compressed. Raster images are converted to WebP.
    /// </summary>
    public async Task<ImageResult> GetImageResultAsync(string imageId, int? maxWidth, int? maxHeight, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct = default)
    {
        string svgKey = $"img:{imageId}:svg";

        if (cache.TryGetValue(svgKey, out byte[]? cachedSvg))
        {
            return ImageResult.SvgGzipped(cachedSvg!);
        }

        byte[] raw = await GetOrFetchRawAsync(imageId, fetchOriginal, ct);

        if (IsSvg(raw))
        {
            return CacheAndReturnSvg(svgKey, imageId, raw);
        }

        byte[] normalized = await GetOrCreateNormalizedOriginalAsync(imageId, fetchOriginal, ct);

        if (maxWidth is null && maxHeight is null)
        {
            return ImageResult.WebP(normalized);
        }

        byte[] thumbnail = await GetOrCreateThumbnailAsync(imageId, maxWidth, maxHeight, normalized, ct);
        return ImageResult.WebP(thumbnail);
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
        image.SaveAsWebp(output, new WebpEncoder { Quality = quality ?? options.WebPQuality });
        return output.ToArray();
    }

    #region Caching Helpers

    private ImageResult CacheAndReturnSvg(string key, string imageId, byte[] raw)
    {
        byte[] compressed = GzipCompress(raw);
        CacheWithOptions(key, compressed, options.CacheLifetime, CacheItemPriority.High);
        logger.LogDebug("Cached SVG {ImageId}: {Size}KB (gzipped)", imageId, compressed.Length / 1024);
        return ImageResult.SvgGzipped(compressed);
    }

    private async Task<byte[]> GetOrFetchRawAsync(string imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct)
    {
        string key = $"img:{imageId}:raw";

        if (cache.TryGetValue(key, out byte[]? cached))
        {
            return cached!;
        }

        byte[] raw = await fetchOriginal(ct);
        ValidateNotEmpty(raw, imageId);

        // Short TTL - just to dedupe concurrent requests
        CacheWithOptions(key, raw, TimeSpan.FromSeconds(30));
        return raw;
    }

    private async Task<byte[]> GetOrCreateNormalizedOriginalAsync(string imageId, Func<CancellationToken, Task<byte[]>> fetchOriginal, CancellationToken ct)
    {
        string key = $"img:{imageId}:original";

        if (cache.TryGetValue(key, out byte[]? cached))
        {
            return cached!;
        }

        return await CoalescedCreateAsync(key, async () =>
        {
            logger.LogDebug("Fetching original for {ImageId}", imageId);

            byte[] raw = await fetchOriginal(ct);
            ValidateNotEmpty(raw, imageId);

            byte[] normalized = Resize(raw, options.MaxOriginalDimension, options.MaxOriginalDimension, options.OriginalWebPQuality);
            CacheWithOptions(key, normalized, options.CacheLifetime, CacheItemPriority.High);

            logger.LogDebug("Cached {ImageId}: {Size}KB", imageId, normalized.Length / 1024);
            return normalized;
        }, ct);
    }

    private async Task<byte[]> GetOrCreateThumbnailAsync(string imageId, int? maxWidth, int? maxHeight, byte[] normalizedOriginal, CancellationToken ct)
    {
        string key = $"img:{imageId}:{maxWidth ?? 0}x{maxHeight ?? 0}";

        if (cache.TryGetValue(key, out byte[]? cached))
        {
            return cached!;
        }

        return await CoalescedCreateAsync(key, async () =>
        {
            byte[] thumbnail = await Task.Run(() => Resize(normalizedOriginal, maxWidth, maxHeight, options.WebPQuality));
            CacheWithOptions(key, thumbnail, options.CacheLifetime);

            logger.LogDebug("Cached thumbnail {Key}: {Size}KB", key, thumbnail.Length / 1024);
            return thumbnail;
        }, ct);
    }

    /// <summary>
    /// Coalesces concurrent requests for the same key into a single execution.
    /// </summary>
    private async Task<byte[]> CoalescedCreateAsync(string key, Func<Task<byte[]>> createAsync, CancellationToken ct)
    {
        Task<byte[]> task = _pendingTasks.GetOrAdd(key, _ => ExecuteAndRemoveAsync(key, createAsync));
        return await task.WaitAsync(ct);
    }

    private async Task<byte[]> ExecuteAndRemoveAsync(string key, Func<Task<byte[]>> createAsync)
    {
        try
        {
            return await createAsync();
        }
        finally
        {
            _pendingTasks.TryRemove(key, out _);
        }
    }

    private void CacheWithOptions(string key, byte[] data, TimeSpan lifetime, CacheItemPriority priority = CacheItemPriority.Normal)
    {
        cache.Set(key, data, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(lifetime)
            .SetPriority(priority)
            .SetSize(data.Length));
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

    private static bool IsSvg(byte[] data)
    {
        if (data.Length < 5)
        {
            return false;
        }

        int offset = (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) ? 3 : 0;
        string start = Encoding.UTF8.GetString(data, offset, Math.Min(256, data.Length - offset)).TrimStart();

        return (start.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && start.Contains("<svg", StringComparison.OrdinalIgnoreCase))
            || start.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
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
