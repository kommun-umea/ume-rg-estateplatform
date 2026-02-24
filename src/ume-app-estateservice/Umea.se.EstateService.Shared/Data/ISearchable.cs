using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Data;

public interface ISearchable
{
    int Id { get; }
    string Name { get; }
    string? PopularName { get; }
    AddressModel? Address { get; }
    GeoPointModel? GeoLocation { get; }
}
