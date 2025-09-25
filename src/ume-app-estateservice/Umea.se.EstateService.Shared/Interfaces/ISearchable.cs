using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Interfaces;

public interface ISearchable
{
    int Id { get; }
    string Name { get; }
    string? PopularName { get; }
    AddressModel? Address { get; }
    GeoPointModel? GeoLocation { get; }
    DateTimeOffset UpdatedAt { get; }
}
