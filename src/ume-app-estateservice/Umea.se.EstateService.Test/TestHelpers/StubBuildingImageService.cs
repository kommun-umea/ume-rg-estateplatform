using Umea.se.EstateService.Logic.Handlers.Images;
using Umea.se.EstateService.Logic.Models;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Simple stub for <see cref="IBuildingImageService"/> that returns configured results.
/// Use this for controller tests where you just need to verify the response handling.
/// </summary>
public sealed class StubBuildingImageService : IBuildingImageService
{
    /// <summary>
    /// The result to return from <see cref="GetImageResultAsync"/>.
    /// Set to null to simulate no image found.
    /// </summary>
    public ImageResult? ImageResult { get; set; }

    /// <summary>
    /// The result to return from <see cref="GetImageMetadataAsync"/>.
    /// Set to null to simulate no images found.
    /// </summary>
    public BuildingImageMetadata? MetadataResult { get; set; }

    /// <summary>
    /// Captured building IDs from <see cref="GetImageResultAsync"/> calls.
    /// </summary>
    public List<int> ImageRequestedForBuildingIds { get; } = [];

    /// <summary>
    /// Captured building IDs from <see cref="GetImageMetadataAsync"/> calls.
    /// </summary>
    public List<int> MetadataRequestedForBuildingIds { get; } = [];

    /// <summary>
    /// Captured building IDs from <see cref="InvalidateCacheAsync"/> calls.
    /// </summary>
    public List<int> CacheInvalidatedForBuildingIds { get; } = [];

    public Task<ImageResult?> GetImageResultAsync(
        int buildingId,
        int? imageId,
        int? maxWidth,
        int? maxHeight,
        CancellationToken cancellationToken = default)
    {
        ImageRequestedForBuildingIds.Add(buildingId);
        return Task.FromResult(ImageResult);
    }

    public Task<BuildingImageMetadata?> GetImageMetadataAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        MetadataRequestedForBuildingIds.Add(buildingId);
        return Task.FromResult(MetadataResult);
    }

    public Task InvalidateCacheAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        CacheInvalidatedForBuildingIds.Add(buildingId);
        return Task.CompletedTask;
    }

    public Task PreWarmImageAsync(
        int buildingId,
        int? imageId,
        IReadOnlyList<ImageVariantRequest> variants,
        CancellationToken cancellationToken = default)
    {
        ImageRequestedForBuildingIds.Add(buildingId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets all configured results and captured requests.
    /// </summary>
    public void Reset()
    {
        ImageResult = null;
        MetadataResult = null;
        ImageRequestedForBuildingIds.Clear();
        MetadataRequestedForBuildingIds.Clear();
        CacheInvalidatedForBuildingIds.Clear();
    }
}
