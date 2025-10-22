using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving floors within a building.
/// </summary>
public sealed record BuildingFloorsRequest : PagedQueryRequest
{
    /// <summary>
    /// Indicates whether room data should be included for each floor.
    /// </summary>
    [FromQuery(Name = "includeRooms")]
    [SwaggerParameter("When true rooms for each floor are included in the response.", Required = false)]
    public bool IncludeRooms { get; init; } = false;
}
