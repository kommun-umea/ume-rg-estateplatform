using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

/// <summary>
/// Service for building image operations with caching and resizing support.
/// </summary>
public interface IBuildingImageService
{
    /// <summary>
    /// Gets an image for a building with optional resizing.
    /// Images are cached and served as WebP format.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="imageId">Optional image ID. If null, returns the primary (most recently updated) image.</param>
    /// <param name="maxWidth">Optional maximum width in pixels</param>
    /// <param name="maxHeight">Optional maximum height in pixels</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image bytes as WebP, or null if no images exist or image doesn't belong to the building</returns>
    Task<byte[]?> GetImageAsync(int buildingId, int? imageId, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata about all images for a building.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image metadata including primary image ID and all image IDs, or null if no images exist</returns>
    Task<BuildingImagesResponse?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached metadata for a building.
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCacheAsync(int buildingId, CancellationToken cancellationToken = default);
}
