using System.Diagnostics.CodeAnalysis;
using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingInfoModel : ISearchable
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public GeoPointModel? GeoLocation { get; init; }
    public decimal GrossArea { get; init; }
    public decimal NetArea { get; init; }
    public decimal SumGrossFloorArea { get; init; }
    public int NumPlacedPersons { get; init; }
    public AddressModel? Address { get; init; }
    public IReadOnlyDictionary<string, string?> ExtraInfo { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> PropertyValues { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> NavigationInfo { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    [MemberNotNullWhen(true, nameof(GeoLocation))]
    public bool HasGeoLocation => GeoLocation is not null;

    public DateTimeOffset UpdatedAt => DateTime.Now;
}
