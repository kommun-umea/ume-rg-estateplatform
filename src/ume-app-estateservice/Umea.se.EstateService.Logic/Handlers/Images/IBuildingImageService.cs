using Umea.se.EstateService.Logic.Models;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Handlers.Images;

/// <summary>
/// Service for building image operations with caching and resizing support.
/// </summary>
public interface IBuildingImageService
{
    /// <summary>
    /// Gets an image for a building with optional resizing.
    /// SVGs are GZip compressed and returned as-is. Raster images are converted to WebP.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="imageId">Optional image ID. If null, returns the primary (most recently updated) image.</param>
    /// <param name="maxWidth">Optional maximum width in pixels</param>
    /// <param name="maxHeight">Optional maximum height in pixels</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image result with data and content type, or null if no images exist or image doesn't belong to the building</returns>
    Task<ImageResult?> GetImageResultAsync(int buildingId, int? imageId, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh cache entries for a building's image without using the user-facing
    /// GetOrSetAsync factory path. Used by the nightly pre-warm job. Silently
    /// returns when the building or image does not exist or the image does not
    /// belong to the building.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="imageId">Optional image ID. If null, pre-warms the primary image.</param>
    /// <param name="variants">Variants to cache. A variant with both dimensions null refers to the normalized original.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PreWarmImageAsync(int buildingId, int? imageId, IReadOnlyList<ImageVariantRequest> variants, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about all images for a building.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image metadata including primary image ID and all image IDs, or null if no images exist</returns>
    Task<BuildingImageMetadata?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached metadata for a building.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCacheAsync(int buildingId, CancellationToken cancellationToken = default);
}
