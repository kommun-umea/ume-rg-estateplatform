using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving building information.
/// </summary>
public sealed record BuildingListRequest : PagedQueryRequest
{
    /// <summary>
    /// Filters buildings by estate (navigation folder) when provided.
    /// </summary>
    [FromQuery(Name = "estateId")]
    [Range(1, int.MaxValue, ErrorMessage = "EstateId must be positive.")]
    [SwaggerParameter("Optional estate identifier used to scope buildings.", Required = false)]
    public int? EstateId { get; init; }
}
