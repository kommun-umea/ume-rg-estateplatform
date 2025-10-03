using Umea.se.Toolkit.Controllers;

namespace Umea.se.EstateService.API;

public class ApiRoutes : ApiRoutesBase
{
    public const string Estates = $"{RoutePrefixV1}/estates";
    public const string Buildings = $"{RoutePrefixV1}/buildings";
    public const string Rooms = $"{RoutePrefixV1}/rooms";
    public const string Autocomplete = $"{RoutePrefixV1}/autocomplete";
    public const string Search = $"{RoutePrefixV1}/search";
    public const string Admin = $"{RoutePrefixV1}/admin";
    public const string EstateBuildings = $"{Estates}/{{estateId:int}}/buildings";
}
