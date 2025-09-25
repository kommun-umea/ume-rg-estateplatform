using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingInfoModel
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public MarkerTypeEnum MarkerType { get; init; }
    public GeoPointModel? GeoLocation { get; init; }
    public decimal GrossArea { get; init; }
    public decimal NetArea { get; init; }
    public decimal SumGrossFloorArea { get; init; }
    public int NumPlacedPersons { get; init; }
    public string AddressName { get; init; } = string.Empty;
    public AddressModel Address { get; init; } = AddressModel.Empty;
    public string Origin { get; init; } = string.Empty;
    public int? CurrencyId { get; init; }
    public string? CurrencyName { get; init; }
    public IReadOnlyList<int> FlagStatusIds { get; init; } = Array.Empty<int>();
    public int? BusinessTypeId { get; init; }
    public string? BusinessTypeName { get; init; }
    public int? ProspectOfBuildingId { get; init; }
    public bool IsProspect { get; init; }
    public long? ProspectStartDate { get; init; }
    public IReadOnlyDictionary<string, string?> ExtraInfo { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> PropertyValues { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> NavigationInfo { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    [MemberNotNullWhen(true, nameof(GeoLocation))]
    public bool HasGeoLocation => GeoLocation is not null;
}
