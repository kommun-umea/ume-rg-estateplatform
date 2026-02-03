using System.Globalization;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Umea.se.Toolkit.Images.Caching;

/// <summary>
/// IDistributedCache implementation backed by Azure Blob Storage.
/// Used as L2 cache for FusionCache.
/// </summary>
public sealed class BlobDistributedCache(BlobContainerClient container, ILogger<BlobDistributedCache> logger) : IDistributedCache
{
    private readonly BlobContainerClient _container = container;
    private readonly ILogger<BlobDistributedCache> _logger = logger;

    public byte[]? Get(string key)
    {
        return GetAsync(key).GetAwaiter().GetResult();
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient(KeyToPath(key));
            Response<BlobDownloadResult> response = await blob.DownloadContentAsync(token);

            // Check expiration from metadata
            if (response.Value.Details.Metadata.TryGetValue("expiresAt", out string? expiresAtStr)
                && DateTimeOffset.TryParse(expiresAtStr, null, DateTimeStyles.RoundtripKind, out DateTimeOffset expiresAt)
                && expiresAt < DateTimeOffset.UtcNow)
            {
                _logger.LogDebug("Blob cache entry expired: {Key}", key);
                return null;
            }

            return response.Value.Content.ToArray();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob cache get failed: {Key}", key);
            return null;
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient(KeyToPath(key));

            // Calculate TTL from options
            TimeSpan ttl = options.AbsoluteExpirationRelativeToNow
                ?? options.SlidingExpiration
                ?? TimeSpan.FromHours(24);

            DateTimeOffset expiresAt = DateTimeOffset.UtcNow + ttl;

            using MemoryStream stream = new(value);
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/octet-stream",
                    CacheControl = $"public, max-age={(int)ttl.TotalSeconds}"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["expiresAt"] = expiresAt.ToString("O"),
                    ["createdAt"] = DateTimeOffset.UtcNow.ToString("O")
                }
            }, token);

            _logger.LogDebug("Blob cache set: {Key}, expires {ExpiresAt}", key, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob cache set failed: {Key}", key);
            // Don't throw - cache failures shouldn't break the application
        }
    }

    public void Remove(string key)
    {
        RemoveAsync(key).GetAwaiter().GetResult();
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            BlobClient blob = _container.GetBlobClient(KeyToPath(key));
            await blob.DeleteIfExistsAsync(cancellationToken: token);
            _logger.LogDebug("Blob cache removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Blob cache remove failed: {Key}", key);
        }
    }

    public void Refresh(string key)
    {
        // Blob storage doesn't support sliding expiration refresh
        // This is intentionally a no-op
    }

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        // Blob storage doesn't support sliding expiration refresh
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the cache key as the blob path.
    /// Keys are already formatted as valid blob paths by ImageCacheKeys/BlobPathBuilder.
    /// </summary>
    private static string KeyToPath(string key) => key;
}
