using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Common;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public sealed class BuildingImageService(IPythagorasClient pythagorasClient, ILogger<BuildingImageService> logger) : IBuildingImageService
{
    public async Task<BuildingImageResult?> GetPrimaryImageAsync(int buildingId, BuildingImageSize size, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        IReadOnlyList<GalleryImageFile> images = await pythagorasClient
            .GetBuildingGalleryImagesAsync(buildingId, cancellationToken)
            .ConfigureAwait(false);

        if (images.Count == 0)
        {
            logger.LogDebug("No gallery images returned for building {BuildingId}.", buildingId);
            return null;
        }

        GalleryImageFile selected = SelectPrimaryImage(images);
        GalleryImageVariant variant = MapVariant(size);

        BinaryResourceResult? resource = await pythagorasClient
            .GetGalleryImageDataAsync(selected.Id, variant, cancellationToken)
            .ConfigureAwait(false);

        if (resource is null)
        {
            logger.LogInformation("Gallery image {ImageId} for building {BuildingId} returned 404.", selected.Id, buildingId);
            return null;
        }

        return new BuildingImageResult(
            resource.Content,
            resource.ContentType,
            selected.Name,
            resource.Length,
            selected.Id,
            resource);
    }

    private static GalleryImageVariant MapVariant(BuildingImageSize size) => size switch
    {
        BuildingImageSize.Thumbnail => GalleryImageVariant.Thumbnail,
        BuildingImageSize.Original => GalleryImageVariant.Original,
        _ => GalleryImageVariant.Original
    };

    private static GalleryImageFile SelectPrimaryImage(IReadOnlyList<GalleryImageFile> images)
    {
        return images
            .OrderByDescending(static i => i.Updated)
            .ThenBy(static i => i.Id)
            .First();
    }
}
