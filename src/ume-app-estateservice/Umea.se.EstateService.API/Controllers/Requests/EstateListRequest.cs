using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving estates.
/// </summary>
public sealed record EstateListRequest : PagedQueryRequest
{
    /// <summary>
    /// Indicates whether to include buildings for each estate in the response.
    /// </summary>
    [FromQuery(Name = "includeBuildings")]
    [SwaggerParameter("When true, includes ancestor building information for each estate.", Required = false)]
    public bool IncludeBuildings { get; init; }
}
