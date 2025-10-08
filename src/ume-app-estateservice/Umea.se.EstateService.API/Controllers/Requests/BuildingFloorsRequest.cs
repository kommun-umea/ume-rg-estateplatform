namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving floors within a building.
/// </summary>
public sealed record BuildingFloorsRequest : PagedQueryRequest;
