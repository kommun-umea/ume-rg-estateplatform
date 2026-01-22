namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Represents metadata about a building's images.
/// </summary>
public sealed class BuildingImageMetadata(int buildingId, int primaryImageId, IReadOnlyList<int> allImageIds)
{
    public int BuildingId { get; init; } = buildingId;
    public int PrimaryImageId { get; init; } = primaryImageId;
    public IReadOnlyList<int> AllImageIds { get; init; } = allImageIds;
    public DateTime CachedAt { get; init; } = DateTime.UtcNow;
}
