using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class EstateModel
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public decimal GrossArea { get; init; }
    public decimal NetArea { get; init; }
    public int NavigationId { get; init; }
    public string NavigationName { get; init; } = string.Empty;
    public GeoPointModel? GeoLocation { get; init; }
    public AddressModel Address { get; init; } = AddressModel.Empty;
}
