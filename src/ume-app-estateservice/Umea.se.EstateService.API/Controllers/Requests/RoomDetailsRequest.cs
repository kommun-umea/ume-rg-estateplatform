using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving a single room.
/// </summary>
public sealed record RoomDetailsRequest
{
    /// <summary>
    /// Indicates whether to include building information for the room.
    /// </summary>
    [FromQuery(Name = "includeBuilding")]
    [SwaggerParameter("When true, includes building information linked to the room (if available).", Required = false)]
    public bool IncludeBuilding { get; init; }
}
