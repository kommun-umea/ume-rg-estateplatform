using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class RoomModel : ISearchable
{
    public int Id { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public double GrossArea { get; init; }
    public double NetArea { get; init; }
    public double CommonArea { get; init; }
    public double UpliftedArea { get; init; }
    public int Capacity { get; init; }
    public int OptimalCapacity { get; init; }
    public int? BuildingId { get; init; }
    public string? BuildingName { get; init; }
    public string? BuildingPopularName { get; init; }
    public int? FloorId { get; init; }
    public string? FloorName { get; init; }
    public string? FloorPopularName { get; init; }
    public AddressModel? Address => null;
    public GeoPointModel? GeoLocation => null;
    public DateTimeOffset UpdatedAt => DateTimeOffset.FromUnixTimeMilliseconds(Updated / 1000);
}
