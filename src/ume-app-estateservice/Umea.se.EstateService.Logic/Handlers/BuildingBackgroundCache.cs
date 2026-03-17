using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Infrastructure;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Handlers;

/// <summary>
/// Unified in-memory cache for per-building background-fetched properties (document count + image IDs).
/// Both properties share one staleness timestamp and one refresh queue.
/// Stale entries are refreshed in the background; callers always get the cached value immediately.
/// </summary>
public sealed class BuildingBackgroundCache(IPythagorasClient pythagorasClient, IDataStore dataStore, ILogger<BuildingBackgroundCache> logger)
{
    private readonly ConcurrentDictionary<int, CacheEntry> _cache = new();
    private readonly BackgroundRefreshQueue _queue = new("BuildingCache", capacity: 500, maxConcurrency: 5, logger);
    private static readonly TimeSpan _staleThreshold = TimeSpan.FromHours(24);

    /// <summary>
    /// Returns the cached document count, or null if not yet fetched.
    /// Enqueues a background refresh if the entry is stale or missing.
    /// </summary>
    public int? GetDocumentCount(int buildingId)
    {
        if (_cache.TryGetValue(buildingId, out CacheEntry? entry))
        {
            if (IsStale(entry))
            {
                _queue.Enqueue(buildingId);
            }

            return entry.DocumentCount;
        }

        _queue.Enqueue(buildingId);
        return null;
    }

    /// <summary>
    /// Returns the cached image IDs, or null if not yet fetched.
    /// Enqueues a background refresh if the entry is stale or missing.
    /// </summary>
    public IReadOnlyList<int>? GetImageIds(int buildingId)
    {
        if (_cache.TryGetValue(buildingId, out CacheEntry? entry))
        {
            if (IsStale(entry))
            {
                _queue.Enqueue(buildingId);
            }

            return entry.ImageIds;
        }

        _queue.Enqueue(buildingId);
        return null;
    }

    /// <summary>
    /// Returns cached image IDs, or fetches inline from Pythagoras on cache miss.
    /// Unlike <see cref="GetImageIds"/>, this does not enqueue a background refresh on miss —
    /// it IS the fetch. Stale entries still trigger a background refresh.
    /// </summary>
    public async Task<IReadOnlyList<int>?> GetOrFetchImageIdsAsync(int buildingId, CancellationToken ct)
    {
        if (_cache.TryGetValue(buildingId, out CacheEntry? entry))
        {
            if (IsStale(entry))
            {
                _queue.Enqueue(buildingId);
            }

            return entry.ImageIds;
        }

        IReadOnlyList<GalleryImageFile> images = await pythagorasClient.GetBuildingGalleryImagesAsync(buildingId, ct);

        IReadOnlyList<int> sortedIds = [.. images
            .OrderByDescending(static i => i.Updated)
            .ThenBy(static i => i.Id)
            .Select(static i => i.Id)];

        SetImageIds(buildingId, sortedIds);
        return sortedIds;
    }

    /// <summary>
    /// Returns the first (primary) image ID, or null if not yet fetched or no images.
    /// Enqueues a background refresh if the entry is stale or missing.
    /// </summary>
    public int? GetPrimaryImageId(int buildingId)
    {
        IReadOnlyList<int>? imageIds = GetImageIds(buildingId);
        return imageIds is { Count: > 0 } ? imageIds[0] : null;
    }

    /// <summary>
    /// Manually set image IDs for a building (e.g. after an inline fetch in BuildingImageService).
    /// Preserves existing DocumentCount and updates FetchedAtUtc.
    /// </summary>
    public void SetImageIds(int buildingId, IReadOnlyList<int> ids)
    {
        CacheEntry entry = _cache.AddOrUpdate(
            buildingId,
            _ => new CacheEntry
            {
                DocumentCount = null,
                ImageIds = ids,
                FetchedAtUtc = DateTimeOffset.UtcNow
            },
            (_, existing) => new CacheEntry
            {
                DocumentCount = existing.DocumentCount,
                ImageIds = ids,
                FetchedAtUtc = DateTimeOffset.UtcNow
            });

        WriteToSnapshot(buildingId, entry);
    }

    /// <summary>
    /// Seeds the cache from building entities loaded from persistence.
    /// </summary>
    public void SeedFrom(IEnumerable<BuildingEntity> buildings)
    {
        foreach (BuildingEntity b in buildings)
        {
            if (b.BackgroundCacheFetchedAtUtc is null)
            {
                // Also seed buildings with only ImageIds (from before the unified cache existed)
                if (b.ImageIds is { Count: > 0 })
                {
                    _cache[b.Id] = new CacheEntry
                    {
                        DocumentCount = b.NumDocuments,
                        ImageIds = b.ImageIds,
                        FetchedAtUtc = DateTimeOffset.MinValue // force stale to trigger background refresh
                    };
                }

                continue;
            }

            _cache[b.Id] = new CacheEntry
            {
                DocumentCount = b.NumDocuments,
                ImageIds = b.ImageIds ?? [],
                FetchedAtUtc = b.BackgroundCacheFetchedAtUtc.Value
            };
        }
    }

    /// <summary>
    /// Writes cached values back onto building entities before persistence.
    /// </summary>
    public void ApplyTo(IEnumerable<BuildingEntity> buildings)
    {
        foreach (BuildingEntity b in buildings)
        {
            if (!_cache.TryGetValue(b.Id, out CacheEntry? entry))
            {
                continue;
            }

            b.NumDocuments = entry.DocumentCount;
            b.ImageIds = entry.ImageIds;
            b.BackgroundCacheFetchedAtUtc = entry.FetchedAtUtc;
        }
    }

    /// <summary>
    /// Starts the background consumer loop that processes stale building refreshes.
    /// </summary>
    public Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        return _queue.RunConsumerAsync(RefreshBuildingAsync, stoppingToken);
    }

    private async Task RefreshBuildingAsync(int buildingId, CancellationToken ct)
    {
        // Fetch both properties in parallel
        Task<UiListDataResponse<FileDocument>> docTask =
            pythagorasClient.GetBuildingDocumentListAsync(buildingId, maxResults: 0, ct);
        Task<IReadOnlyList<GalleryImageFile>> imgTask =
            pythagorasClient.GetBuildingGalleryImagesAsync(buildingId, ct);

        await Task.WhenAll(docTask, imgTask);

        int docCount = docTask.Result.TotalSize;
        IReadOnlyList<int> imageIds = [.. imgTask.Result
            .OrderByDescending(i => i.Updated)
            .ThenBy(i => i.Id)
            .Select(i => i.Id)];

        CacheEntry entry = new()
        {
            DocumentCount = docCount,
            ImageIds = imageIds,
            FetchedAtUtc = DateTimeOffset.UtcNow
        };

        _cache[buildingId] = entry;
        WriteToSnapshot(buildingId, entry);
    }

    /// <summary>
    /// Write-through: mutates the live snapshot entity so consumers reading
    /// building.ImageIds / building.NumDocuments see fresh values immediately.
    /// </summary>
    private void WriteToSnapshot(int buildingId, CacheEntry entry)
    {
        if (dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? b))
        {
            b.ImageIds = entry.ImageIds;
            b.NumDocuments = entry.DocumentCount;
            b.BackgroundCacheFetchedAtUtc = entry.FetchedAtUtc;
        }
    }

    private static bool IsStale(CacheEntry entry)
        => DateTimeOffset.UtcNow - entry.FetchedAtUtc > _staleThreshold;

    private sealed record CacheEntry
    {
        public required int? DocumentCount { get; init; }
        public required IReadOnlyList<int> ImageIds { get; init; }
        public required DateTimeOffset FetchedAtUtc { get; init; }
    }
}
