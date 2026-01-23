using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.API.Controllers;

/// <summary>
/// Controller for building image operations
/// </summary>
[ApiController]
[Route(ApiRoutes.BuildingImages)]
[Authorize]
public class BuildingImagesController(IBuildingImageService buildingImageService) : ControllerBase
{
    private static readonly int[] _allowedSizes = [150, 300, 600, 900, 1200];

    private static int? SnapToAllowedSize(int? requested)
    {
        if (requested is null)
        {
            return null;
        }

        int snapped = _allowedSizes.FirstOrDefault(s => s >= requested);
        return snapped > 0 ? snapped : _allowedSizes[^1];
    }

    /// <summary>
    /// Gets all image IDs for a building
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of image IDs</returns>
    /// <response code="200">Returns the list of image IDs</response>
    /// <response code="404">Building has no images</response>
    [HttpGet("images")]
    [SwaggerOperation(
        Summary = "Get all image IDs for a building",
        Description = "Retrieves the IDs of all gallery images for the specified building. Use these IDs with /api/buildings/{buildingId}/image?imageId={id} to fetch specific images."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of image IDs", typeof(BuildingImagesResponse))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Building has no images")]
    public async Task<ActionResult<BuildingImagesResponse>> GetBuildingImageIds(int buildingId, CancellationToken cancellationToken)
    {
        if (buildingId <= 0)
        {
            return BadRequest("Building id must be positive.");
        }

        BuildingImagesResponse? response = await buildingImageService.GetImageMetadataAsync(buildingId, cancellationToken);

        if (response is null)
        {
            return NotFound(new { message = "No images found for this building" });
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets an image for a building with optional resizing
    /// </summary>
    /// <param name="buildingId">The building ID</param>
    /// <param name="imageId">Optional image ID. If not specified, returns the primary image.</param>
    /// <param name="w">Optional maximum width in pixels. Snapped to nearest allowed size: 150, 300, 600, 900, 1200</param>
    /// <param name="h">Optional maximum height in pixels. Snapped to nearest allowed size: 150, 300, 600, 900, 1200</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The image as WebP</returns>
    [AllowAnonymous]
    [HttpGet("image")]
    [SwaggerOperation(
        Summary = "Get an image for a building",
        Description = "Returns an image for the building. If imageId is not specified, returns the primary (most recently updated) image. Supports optional resizing via w/h query parameters (snapped to allowed sizes: 150, 300, 600, 900, 1200). Returns WebP format."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "The image", ContentTypes = ["image/webp"])]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Building has no images or image not found")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid parameters or image too large")]
    public async Task<IActionResult> GetBuildingImage(int buildingId, [FromQuery] int? imageId, [FromQuery] int? w, [FromQuery] int? h, CancellationToken cancellationToken)
    {
        if (buildingId <= 0)
        {
            return BadRequest("Building id must be positive.");
        }

        if (w is < 0)
        {
            return BadRequest("Width must be positive.");
        }

        if (h is < 0)
        {
            return BadRequest("Height must be positive.");
        }

        // Treat 0 as "no constraint"
        if (w is 0)
        {
            w = null;
        }

        if (h is 0)
        {
            h = null;
        }

        // Snap to allowed sizes to limit cache variations
        w = SnapToAllowedSize(w);
        h = SnapToAllowedSize(h);

        if (imageId is < 0)
        {
            return BadRequest("Image id must be positive.");
        }

        try
        {
            byte[]? imageData = await buildingImageService.GetImageAsync(buildingId, imageId, w, h, cancellationToken);

            if (imageData is null)
            {
                return NotFound(new { message = imageId.HasValue ? "Image not found" : "No images found for this building" });
            }

            Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromHours(24)
            };

            return File(imageData, "image/webp");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound(new { message = "Image not found" });
        }
        catch (ImageNotFoundException)
        {
            return NotFound(new { message = "Image not found" });
        }
        catch (ImageTooLargeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
