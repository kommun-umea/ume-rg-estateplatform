namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

/// <summary>
/// Work order types defined in Pythagoras.
/// Integer values match the Pythagoras type IDs.
/// </summary>
public enum PythagorasWorkOrderType
{
    ErrorReport = 1,           // Felanmälan
    BuildingService = 2,       // Byggserviceärende
    SpaceRequirement = 3,      // Förändrade lokalbehov
    TosRemark = 4,             // ToS-anmärkning
    InspectionRemark = 5,      // Besiktningsanmärkning
    FacilityService = 8,       // Verksamhetsvaktmästare
    TownHallService = 9,       // Stadshusservice
}
