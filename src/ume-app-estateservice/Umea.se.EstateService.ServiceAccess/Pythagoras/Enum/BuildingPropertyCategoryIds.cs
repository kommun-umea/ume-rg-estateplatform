namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

/// <summary>
/// Property category identifiers for Pythagoras assets owned by buildings.
/// Source: /rest/v1/propertycategory (ownerType = BUILDING).
/// </summary>
public static class BuildingPropertyCategoryIds
{
    public const int Drawings = 2; // Ritningar
    public const int BuildingInformation = 4; // Byggnadsinformation
    public const int OperationsGroups = 8; // Driftgrupper
    public const int EstatePortalNoticeBoard = 34; // Anslagstavla Fastighetsportal
}
