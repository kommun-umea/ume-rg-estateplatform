namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

/// <summary>
/// Property category identifiers for Pythagoras assets owned by buildings.
/// Source: /rest/v1/propertycategory (ownerType = BUILDING).
/// </summary>
public enum BuildingPropertyCategoryId
{
    Drawings = 2,
    BuildingInformation = 4,
    OperationsGroups = 8,
    EstatePortalNoticeBoard = 34
}
