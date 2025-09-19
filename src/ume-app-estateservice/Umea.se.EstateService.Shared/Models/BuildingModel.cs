using System.Diagnostics.CodeAnalysis;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingModel
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PopularName { get; init; } = string.Empty;
    public MarkerType MarkerType { get; init; }
    public GeoPointModel GeoLocation { get; init; } = new();
    public string Origin { get; init; } = string.Empty;
    public decimal PropertyTax { get; init; }
    public bool UseWeightsInWorkspaceAreaDistribution { get; init; }

    [MemberNotNullWhen(true, nameof(GeoLocation))]
    public bool HasGeoLocation => GeoLocation is not null;
}
