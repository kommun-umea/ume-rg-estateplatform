using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Fake implementation that bypasses image processing for tests.
/// Returns raw bytes from Pythagoras without ImageSharp processing.
/// </summary>
public sealed class FakeBuildingImageService(IPythagorasClient pythagorasClient) : IBuildingImageService
{
    public async Task<byte[]?> GetImageAsync(int buildingId, int? imageId, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GalleryImageFile> images = await pythagorasClient
            .GetBuildingGalleryImagesAsync(buildingId, cancellationToken);

        if (images.Count == 0)
        {
            return null;
        }

        int resolvedImageId;
        if (imageId.HasValue)
        {
            if (!images.Any(i => i.Id == imageId.Value))
            {
                return null;
            }
            resolvedImageId = imageId.Value;
        }
        else
        {
            resolvedImageId = images
                .OrderByDescending(i => i.Updated)
                .ThenBy(i => i.Id)
                .First()
                .Id;
        }

        using HttpResponseMessage response = await pythagorasClient
            .GetGalleryImageDataAsync(resolvedImageId, GalleryImageVariant.Original, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<BuildingImagesResponse?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<GalleryImageFile> images = await pythagorasClient
            .GetBuildingGalleryImagesAsync(buildingId, cancellationToken);

        if (images.Count == 0)
        {
            return null;
        }

        GalleryImageFile primaryImage = images
            .OrderByDescending(i => i.Updated)
            .ThenBy(i => i.Id)
            .First();

        return new BuildingImagesResponse
        {
            BuildingId = buildingId,
            PrimaryImageId = primaryImage.Id,
            ImageIds = [.. images.Select(i => i.Id)],
            TotalCount = images.Count
        };
    }

    public Task InvalidateCacheAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
