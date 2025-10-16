namespace Umea.se.EstateService.Shared.Models;

[Flags]
public enum BuildingIncludeOptions
{
    None = 0,
    ExtendedProperties = 1 << 0
}
