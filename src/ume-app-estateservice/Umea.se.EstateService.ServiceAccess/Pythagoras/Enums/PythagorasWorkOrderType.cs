namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

/// <summary>
/// Work order types defined in Pythagoras.
/// Integer values match the Pythagoras type IDs.
/// </summary>
public enum PythagorasWorkOrderType
{
    ErrorReport = 1,        // Felanmälan
    BuildingService = 2,    // Byggserviceärende
    SpaceRequirement = 3    // Förändrade lokalbehov
}
