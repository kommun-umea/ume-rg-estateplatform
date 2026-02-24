using System.Collections.Concurrent;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Handlers.Images;

/// <summary>
/// Thread-safe cache of building image IDs backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Lazily populated per-building from Pythagoras and persisted to SQL via <see cref="BuildingEntity.ImageIds"/>.
/// </summary>
public sealed class BuildingImageIdCache
{
    private readonly ConcurrentDictionary<int, IReadOnlyList<int>> _cache = new();

    public Task<int?> GetPrimaryImageIdAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(buildingId, out IReadOnlyList<int>? imageIds) && imageIds.Count > 0)
        {
            return Task.FromResult<int?>(imageIds[0]);
        }

        return Task.FromResult<int?>(null);
    }

    public Task<IReadOnlyList<int>?> GetAllImageIdsAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(buildingId, out IReadOnlyList<int>? imageIds))
        {
            return Task.FromResult<IReadOnlyList<int>?>(imageIds);
        }

        return Task.FromResult<IReadOnlyList<int>?>(null);
    }

    public Task SetImageMetadataAsync(int buildingId, IReadOnlyList<int> allImageIds, int? primaryImageId = null, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<int> ordered;
        if (primaryImageId.HasValue && allImageIds.Count > 0 && allImageIds[0] != primaryImageId.Value)
        {
            ordered = [primaryImageId.Value, .. allImageIds.Where(id => id != primaryImageId.Value)];
        }
        else
        {
            ordered = [.. allImageIds];
        }

        _cache[buildingId] = ordered;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(buildingId, out _);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds the cache from building entities loaded from persistence (e.g. SQL Server).
    /// </summary>
    public void SeedFrom(IEnumerable<BuildingEntity> buildings)
    {
        foreach (BuildingEntity building in buildings)
        {
            if (building.ImageIds is { Count: > 0 })
            {
                _cache[building.Id] = building.ImageIds;
            }
        }
    }

    /// <summary>
    /// Writes cached image IDs back onto building entities before persistence.
    /// </summary>
    public void ApplyTo(IEnumerable<BuildingEntity> buildings)
    {
        foreach (BuildingEntity building in buildings)
        {
            if (_cache.TryGetValue(building.Id, out IReadOnlyList<int>? imageIds))
            {
                building.ImageIds = [.. imageIds];
            }
        }
    }
}
