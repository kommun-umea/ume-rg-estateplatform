namespace Umea.se.EstateService.Shared.Enums;

[Flags]
public enum BuildingIncludeOptions
{
    None = 0,
    ExtendedProperties = 1 << 0
}
