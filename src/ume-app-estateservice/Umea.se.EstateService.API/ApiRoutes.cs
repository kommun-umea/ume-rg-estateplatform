using Umea.se.Toolkit.Controllers;

namespace Umea.se.EstateService.API;

public class ApiRoutes : ApiRoutesBase
{
    public const string Estates = $"{RoutePrefixV1}/estates";
    public const string Buildings = $"{RoutePrefixV1}/buildings";
    public const string BuildingImages = Buildings + "/{buildingId:int}";
    public const string Documents = $"{RoutePrefixV1}/documents";
    public const string Rooms = $"{RoutePrefixV1}/rooms";
    public const string Floors = $"{RoutePrefixV1}/floors";
    public const string Search = $"{RoutePrefixV1}/search";
    public const string BusinessTypes = $"{RoutePrefixV1}/businessTypes";
    public const string Admin = $"{RoutePrefixV1}/admin";
    public const string EstateBuildings = $"{Estates}/{{estateId:int}}/buildings";
}
