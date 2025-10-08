namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving rooms within a building.
/// </summary>
public sealed record class BuildingRoomsRequest : PagedQueryRequest;
