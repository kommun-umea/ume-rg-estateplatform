using System.Text.Json.Serialization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

/// <summary>
/// Bound object types for work orders in Pythagoras.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkOrderBoundObjectType
{
    REALESTATE,
    BUILDING,
    OUTDOOR,
    FLOOR,
    WORKSPACE,
    ASSET
}
