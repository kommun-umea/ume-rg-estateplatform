using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingModel : ISearchable
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public AddressModel? Address { get; set; }
    public GeoPointModel? GeoLocation { get; init; }
    public decimal PropertyTax { get; init; }

    public DateTimeOffset UpdatedAt => DateTimeOffset.FromUnixTimeMilliseconds(Updated / 1000);
}
