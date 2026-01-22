namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Response model for building images endpoint
/// </summary>
public sealed class BuildingImagesResponse
{
    /// <summary>
    /// The building ID
    /// </summary>
    public int BuildingId { get; init; }

    /// <summary>
    /// The primary (most recent) image ID
    /// </summary>
    public int? PrimaryImageId { get; init; }

    /// <summary>
    /// All image IDs for this building
    /// </summary>
    public IReadOnlyList<int> ImageIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Total count of images
    /// </summary>
    public int TotalCount { get; init; }
}
