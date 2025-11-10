using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingLocationModel
{
    public int Id { get; init; }
    public GeoPointModel? GeoLocation { get; init; }
}
