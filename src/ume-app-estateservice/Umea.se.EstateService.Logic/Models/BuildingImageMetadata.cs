namespace Umea.se.EstateService.Logic.Models;

/// <summary>
/// Service-level result for building image metadata.
/// </summary>
public sealed record BuildingImageMetadata(
    int BuildingId,
    int? PrimaryImageId,
    IReadOnlyList<int> ImageIds);
