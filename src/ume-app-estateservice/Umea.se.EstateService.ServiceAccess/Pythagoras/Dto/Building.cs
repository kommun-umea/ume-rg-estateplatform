using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class Building
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = "";
    public string PopularName { get; init; } = "";
    public PythMarkerType MarkerType { get; init; }
    public GeoPoint? GeoLocation { get; init; }
    public string Origin { get; init; } = "";
    public decimal PropertyTax { get; init; }
    public bool UseWeightsInWorkspaceAreaDistribution { get; init; }
}
