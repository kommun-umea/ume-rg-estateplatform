namespace Umea.se.EstateService.Logic.Interfaces;

/// <summary>
/// Provides fast lookup of image metadata for buildings.
/// Implementations can use in-memory cache, Redis, or other storage.
/// </summary>
public interface IBuildingImageMetadataCache
{
    /// <summary>
    /// Gets the primary image ID for a building, or null if not cached.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The primary image ID, or null if not found in cache</returns>
    Task<int?> GetPrimaryImageIdAsync(int buildingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all image IDs for a building, or null if not cached.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all image IDs, or null if not found in cache</returns>
    Task<IReadOnlyList<int>?> GetAllImageIdsAsync(int buildingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches the image metadata for a building.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="allImageIds">All image IDs for the building</param>
    /// <param name="primaryImageId">The primary image ID (defaults to first image if null)</param>
    /// <param name="expiration">Optional expiration time (defaults to implementation-specific value)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetImageMetadataAsync(int buildingId, IReadOnlyList<int> allImageIds, int? primaryImageId = null, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a building's cached image metadata.
    /// </summary>
    /// <param name="buildingId">The building ID to invalidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateAsync(int buildingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
