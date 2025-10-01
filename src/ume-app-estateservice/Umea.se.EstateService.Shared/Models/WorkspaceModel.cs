using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class WorkspaceModel : ISearchable
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public double GrossArea { get; init; }
    public double NetArea { get; init; }
    public double CommonArea { get; init; }

    public int? BuildingId { get; init; }
    public Guid? BuildingUid { get; init; }
    public string? BuildingName { get; init; }
    public string? BuildingPopularName { get; set; }

    public AddressModel? Address => null;
    public GeoPointModel? GeoLocation => null;
    public DateTimeOffset UpdatedAt => DateTimeOffset.FromUnixTimeMilliseconds(Updated / 1000);
}
