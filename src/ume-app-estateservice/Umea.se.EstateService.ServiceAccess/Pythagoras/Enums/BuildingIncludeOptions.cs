namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;

[Flags]
public enum BuildingIncludeOptions
{
    None = 0,
    ExtendedProperties = 1 << 0,
    Ascendants = 1 << 1
}
