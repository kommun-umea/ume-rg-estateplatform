namespace Umea.se.EstateService.Shared.Models;

/// <summary>
/// Represents metadata about a floor returned from Pythagoras.
/// </summary>
public sealed class FloorInfoModel
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public double? Height { get; init; }
    public double? ReferenceHeight { get; init; }
    public double? GrossFloorArea { get; init; }
    public double? GrossArea { get; init; }
    public double? NetArea { get; init; }
    public int? BuildingId { get; init; }
    public Guid? BuildingUid { get; init; }
    public string? BuildingName { get; init; }
    public string? BuildingPopularName { get; init; }
    public string? BuildingOrigin { get; init; }
    public int NumPlacedPersons { get; init; }
    public IReadOnlyList<BuildingRoomModel>? Rooms { get; init; }
}
