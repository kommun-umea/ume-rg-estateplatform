using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class BuildingInfo
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public decimal? Grossarea { get; init; }
    public decimal? Netarea { get; init; }
    public decimal? SumGrossFloorarea { get; init; }
    public int NumPlacedPersons { get; init; }
    public PythMarkerType MarkerType { get; init; }
    public double GeoX { get; init; }
    public double GeoY { get; init; }
    public double GeoRotation { get; init; }
    public string AddressName { get; init; } = string.Empty;
    public string AddressCity { get; init; } = string.Empty;
    public string AddressCountry { get; init; } = string.Empty;
    public string AddressStreet { get; init; } = string.Empty;
    public string AddressZipCode { get; init; } = string.Empty;
    public string AddressExtra { get; init; } = string.Empty;
    public string Origin { get; init; } = string.Empty;
    public int? CurrencyId { get; init; }
    public string? CurrencyName { get; init; }
    public IReadOnlyList<int> FlagStatusIds { get; init; } = Array.Empty<int>();
    public int? BusinessTypeId { get; init; }
    public string? BusinessTypeName { get; init; }
    public int? ProspectOfBuildingId { get; init; }
    public bool IsProspect { get; init; }
    public long? ProspectStartDate { get; init; }
    public Dictionary<string, string?> ExtraInfo { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string?> PropertyValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string?> NavigationInfo { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
