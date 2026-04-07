using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkOrderType
{
    ErrorReport,
    BuildingService,
    TownHallService,
    FacilityService,
}
