using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Handlers;

public sealed class BuildingImageService(IPythagorasClient pythagorasClient, IBuildingImageMetadataCache metadataCache, ImageService imageService, ILogger<BuildingImageService> logger) : IBuildingImageService
{
    public async Task<ImageResult?> GetImageResultAsync(int buildingId, int? imageId, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);
        if (imageId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(imageId.Value);
        }

        int? resolvedImageId = imageId ?? await GetPrimaryImageIdAsync(buildingId, cancellationToken);

        if (resolvedImageId is null)
        {
            return null;
        }

        // If specific imageId was requested, validate it belongs to this building
        if (imageId.HasValue && !await ValidateImageBelongsToBuildingAsync(buildingId, imageId.Value, cancellationToken))
        {
            return null;
        }

        return await imageService.GetImageResultAsync(
            $"images:{resolvedImageId}",
            maxWidth,
            maxHeight,
            fetchOriginal: async ct =>
            {
                using HttpResponseMessage response = await pythagorasClient.GetGalleryImageDataAsync(resolvedImageId.Value, GalleryImageVariant.Original, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(ct);
            },
            cancellationToken);
    }

    public async Task<BuildingImagesResponse?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);

        int? primaryImageId = await GetPrimaryImageIdAsync(buildingId, cancellationToken);
        if (primaryImageId is null)
        {
            return null;
        }

        IReadOnlyList<int>? imageIds = await metadataCache.GetAllImageIdsAsync(buildingId, cancellationToken);

        return new BuildingImagesResponse
        {
            BuildingId = buildingId,
            PrimaryImageId = primaryImageId,
            ImageIds = imageIds ?? [],
            TotalCount = imageIds?.Count ?? 0
        };
    }

    public Task InvalidateCacheAsync(int buildingId, CancellationToken cancellationToken = default)
        => metadataCache.InvalidateAsync(buildingId, cancellationToken);

    private async Task<bool> ValidateImageBelongsToBuildingAsync(int buildingId, int imageId, CancellationToken cancellationToken)
    {
        IReadOnlyList<int>? imageIds = await metadataCache.GetAllImageIdsAsync(buildingId, cancellationToken);

        if (imageIds?.Contains(imageId) == true)
        {
            return true;
        }

        IReadOnlyList<GalleryImageFile> images = await RefreshImageMetadataAsync(buildingId, cancellationToken);
        return images.Any(i => i.Id == imageId);
    }

    private async Task<int?> GetPrimaryImageIdAsync(int buildingId, CancellationToken cancellationToken)
    {
        // Use GetAllImageIdsAsync to detect cache hits - it returns non-null even for empty (no images)
        IReadOnlyList<int>? cachedImageIds = await metadataCache.GetAllImageIdsAsync(buildingId, cancellationToken);
        if (cachedImageIds is not null)
        {
            // Cache hit - return primary from cache (may be null if no images)
            return await metadataCache.GetPrimaryImageIdAsync(buildingId, cancellationToken);
        }

        logger.LogDebug("Fetching image list for building {BuildingId} from Pythagoras", buildingId);

        IReadOnlyList<GalleryImageFile> images = await RefreshImageMetadataAsync(buildingId, cancellationToken);
        return images.Count > 0 ? SelectPrimaryImage(images).Id : null;
    }

    private async Task<IReadOnlyList<GalleryImageFile>> RefreshImageMetadataAsync(int buildingId, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        IReadOnlyList<GalleryImageFile> images = await pythagorasClient.GetBuildingGalleryImagesAsync(buildingId, linkedCts.Token);

        // Always cache the result - even empty arrays (building has no images)
        int? primaryImageId = images.Count > 0 ? SelectPrimaryImage(images).Id : null;
        IReadOnlyList<int> allImageIds = [.. images.Select(i => i.Id)];
        await metadataCache.SetImageMetadataAsync(buildingId, allImageIds, primaryImageId, cancellationToken: cancellationToken);

        return images;
    }

    public static GalleryImageFile SelectPrimaryImage(IReadOnlyList<GalleryImageFile> images)
        => images.OrderByDescending(static i => i.Updated).ThenBy(static i => i.Id).First();
}
