namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

/// <summary>
/// Property category identifiers for Pythagoras assets owned by buildings.
/// Source: /rest/v1/propertycategory (ownerType = BUILDING).
/// </summary>
public enum BuildingPropertyCategoryId
{
    PropertyDesignation = 56,
    YearOfConstruction = 226,
    ExternalOwner = 234,
    Note = 236,
    NoticeBoardText = 239,
    NoticeBoardStartDate = 240,
    NoticeBoardEndDate = 241,
}
