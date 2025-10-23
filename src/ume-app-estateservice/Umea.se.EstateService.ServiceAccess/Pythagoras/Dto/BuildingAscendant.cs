using System;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class BuildingAscendant : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public bool ShowMarker { get; init; }
    public PythMarkerType MarkerType { get; init; }
    public GeoPoint? GeoLocation { get; init; }
    public string Origin { get; init; } = string.Empty;
}
