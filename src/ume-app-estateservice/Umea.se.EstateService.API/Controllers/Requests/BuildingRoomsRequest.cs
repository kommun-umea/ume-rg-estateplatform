using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving rooms within a building.
/// </summary>
public sealed record BuildingRoomsRequest : PagedQueryRequest
{
    /// <summary>
    /// Restricts results to the specified floor within the building.
    /// </summary>
    [FromQuery(Name = "floorId")]
    [Range(1, int.MaxValue, ErrorMessage = "FloorId must be greater than or equal to {1}.")]
    [SwaggerParameter("When provided, only rooms on the specified floor are returned.", Required = false)]
    public int? FloorId { get; init; }
}
