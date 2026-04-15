using System.Net;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Handlers.Images;

public sealed class BuildingImageService(IPythagorasClient pythagorasClient, IDataStore dataStore, ImageService imageService) : IBuildingImageService
{
    private const int MaxConcurrentPythagorasImageFetches = 5;
    private static readonly SemaphoreSlim _pythagorasImageFetchGate =
        new(MaxConcurrentPythagorasImageFetches, MaxConcurrentPythagorasImageFetches);

    public async Task<ImageResult?> GetImageResultAsync(int buildingId, int? imageId, int? maxWidth, int? maxHeight, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);
        if (imageId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(imageId.Value);
        }

        IReadOnlyList<int>? imageIds = GetImageIds(dataStore, buildingId);

        if (imageIds is null or { Count: 0 })
        {
            return null;
        }

        int resolvedImageId = imageId ?? imageIds[0];

        // If specific imageId was requested, validate it belongs to this building
        if (imageId.HasValue && !imageIds.Contains(imageId.Value))
        {
            return null;
        }

        return await imageService.GetImageResultAsync(
            $"images:{resolvedImageId}",
            maxWidth,
            maxHeight,
            fetchOriginal: ct => FetchImageAsync(resolvedImageId, ct),
            cancellationToken);
    }

    public async Task PreWarmImageAsync(int buildingId, int? imageId, IReadOnlyList<ImageVariantRequest> variants, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);
        ArgumentNullException.ThrowIfNull(variants);
        if (imageId.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(imageId.Value);
        }

        if (variants.Count == 0)
        {
            return;
        }

        IReadOnlyList<int>? imageIds = GetImageIds(dataStore, buildingId);

        if (imageIds is null or { Count: 0 })
        {
            return;
        }

        int resolvedImageId = imageId ?? imageIds[0];

        if (imageId.HasValue && !imageIds.Contains(imageId.Value))
        {
            return;
        }

        await imageService.PreWarmImageAsync(
            $"images:{resolvedImageId}",
            fetchOriginal: ct => FetchImageAsync(resolvedImageId, ct),
            variants,
            cancellationToken);
    }

    private async Task<byte[]> FetchImageAsync(int imageId, CancellationToken ct)
    {
        await _pythagorasImageFetchGate.WaitAsync(ct);
        try
        {
            return await FetchImageFromPythagorasAsync(imageId, ct);
        }
        finally
        {
            _pythagorasImageFetchGate.Release();
        }
    }

    private async Task<byte[]> FetchImageFromPythagorasAsync(int imageId, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await pythagorasClient.GetGalleryImageDataAsync(imageId, GalleryImageVariant.Original, ct);
        }
        catch (HttpRequestException ex)
        {
            throw ex.StatusCode == HttpStatusCode.NotFound
                ? new ImageNotFoundException($"Image {imageId} not found in Pythagoras.")
                : new ExternalServiceUnavailableException($"Pythagoras returned {ex.StatusCode} for image {imageId}.", ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            throw new ImageNotFoundException($"Image {imageId} not found in Pythagoras.");
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
    }

    public async Task<BuildingImageMetadata?> GetImageMetadataAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buildingId);

        IReadOnlyList<int>? imageIds = GetImageIds(dataStore, buildingId);

        if (imageIds is null or { Count: 0 })
        {
            return null;
        }

        return new BuildingImageMetadata(buildingId, imageIds[0], imageIds);
    }

    public Task InvalidateCacheAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private static IReadOnlyList<int>? GetImageIds(IDataStore dataStore, int buildingId)
    {
        return dataStore.BuildingsById.TryGetValue(buildingId, out BuildingEntity? building)
            ? building.ImageIds
            : null;
    }
}
