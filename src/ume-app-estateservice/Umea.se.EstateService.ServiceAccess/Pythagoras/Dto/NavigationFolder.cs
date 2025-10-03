namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class NavigationFolder : IPythagorasDto
{
    public int Id { get; init; }
    public string RowId { get; init; } = string.Empty;
    public int NavigationId { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public bool ShowMarker { get; init; }
    public bool IsRealEstate { get; init; }
    public bool IsDistributionNode { get; init; }
    public int MarkerType { get; init; }
    public decimal? Grossarea { get; init; }
    public decimal? Netarea { get; init; }
    public int NumPlacedPersons { get; init; }
    public string NavigationName { get; init; } = string.Empty;
    public string NavigationOrigin { get; init; } = string.Empty;
    public int? GeoPolygonId { get; init; }
    public double GeoX { get; init; }
    public double GeoY { get; init; }
    public double GeoRotation { get; init; }
    public string? AddressCity { get; init; }
    public string? AddressCountry { get; init; }
    public string? AddressStreet { get; init; }
    public string? AddressZipCode { get; init; }
    public string? AddressExtra { get; init; }
    public int? CurrencyId { get; init; }
    public string? CurrencyName { get; init; }
    public int TypeId { get; init; }
    public string TypeName { get; init; } = string.Empty;
    public int TreeLevel { get; init; }
    public int? NavigationNavigationFolderId { get; init; }
    public int? TrusteeId { get; init; }
    public string? TrusteeEmail { get; init; }
    public string? TrusteeFullname { get; init; }
    public int? InheritedTrusteeId { get; init; }
    public string? InheritedTrusteeEmail { get; init; }
    public string? InheritedTrusteeFullname { get; init; }
    public Dictionary<string, string?> PropertyValues { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Building[]? Buildings { get; init; }
}
