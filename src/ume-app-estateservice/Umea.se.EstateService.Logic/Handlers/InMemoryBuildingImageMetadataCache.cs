using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public sealed class InMemoryBuildingImageMetadataCache(
    IMemoryCache cache,
    ILogger<InMemoryBuildingImageMetadataCache> logger,
    int expirationHours = 24) : IBuildingImageMetadataCache
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<InMemoryBuildingImageMetadataCache> _logger = logger;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(expirationHours);

    public Task<int?> GetPrimaryImageIdAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        string key = GetCacheKey(buildingId);

        if (_cache.TryGetValue(key, out BuildingImageMetadata? metadata) && metadata != null)
        {
            _logger.LogDebug("Metadata cache hit for building {BuildingId} → primary image {ImageId}", buildingId, metadata.PrimaryImageId);
            return Task.FromResult<int?>(metadata.PrimaryImageId);
        }

        _logger.LogDebug("Metadata cache miss for building {BuildingId}", buildingId);
        return Task.FromResult<int?>(null);
    }

    public Task<IReadOnlyList<int>?> GetAllImageIdsAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        string key = GetCacheKey(buildingId);

        if (_cache.TryGetValue(key, out BuildingImageMetadata? metadata) && metadata != null)
        {
            _logger.LogDebug("Metadata cache hit for building {BuildingId} → {Count} images", buildingId, metadata.AllImageIds.Count);
            return Task.FromResult<IReadOnlyList<int>?>(metadata.AllImageIds);
        }

        _logger.LogDebug("Metadata cache miss for building {BuildingId}", buildingId);
        return Task.FromResult<IReadOnlyList<int>?>(null);
    }

    public Task SetImageMetadataAsync(int buildingId, IReadOnlyList<int> allImageIds, int? primaryImageId = null, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(allImageIds);

        if (allImageIds.Count == 0)
        {
            throw new ArgumentException("At least one image ID must be provided", nameof(allImageIds));
        }

        string key = GetCacheKey(buildingId);

        // Use first image as primary if not specified
        int resolvedPrimaryImageId = primaryImageId ?? allImageIds[0];

        BuildingImageMetadata metadata = new(buildingId, resolvedPrimaryImageId, allImageIds);

        MemoryCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            Priority = CacheItemPriority.Normal,
            Size = 1 // Metadata entries are small; use minimal size unit
        };

        _cache.Set(key, metadata, options);

        _logger.LogDebug(
            "Cached metadata for building {BuildingId} → primary: {PrimaryImageId}, total: {TotalCount} images (expires in {Expiration})",
            buildingId, resolvedPrimaryImageId, allImageIds.Count, expiration ?? _defaultExpiration);

        return Task.CompletedTask;
    }

    public Task InvalidateAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        string key = GetCacheKey(buildingId);
        _cache.Remove(key);

        _logger.LogInformation("Invalidated metadata cache for building {BuildingId}", buildingId);

        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
            _logger.LogInformation("Metadata cache cleared");
        }
        else
        {
            _logger.LogWarning("ClearAllAsync called but cache implementation does not support Clear()");
        }

        return Task.CompletedTask;
    }

    private static string GetCacheKey(int buildingId) => $"building_image_metadata:{buildingId}";
}
