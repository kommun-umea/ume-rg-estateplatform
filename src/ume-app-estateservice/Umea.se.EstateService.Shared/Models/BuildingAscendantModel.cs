using System.Text.Json.Serialization;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingAscendantModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public GeoPointModel? GeoLocation { get; init; }
    public BuildingAscendantType Type { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BuildingAscendantType
{
    Estate,
    Area,
    Organization
}
