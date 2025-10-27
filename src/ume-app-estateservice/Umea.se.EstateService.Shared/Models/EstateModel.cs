using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class EstateModel : ISearchable
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public decimal GrossArea { get; init; }
    public decimal NetArea { get; init; }
    public GeoPointModel? GeoLocation { get; init; }
    public AddressModel? Address { get; init; }
    public BuildingModel[]? Buildings { get; init; }
    public EstateExtendedPropertiesModel? ExtendedProperties { get; init; }
    public DateTimeOffset UpdatedAt => DateTime.Now;
}
