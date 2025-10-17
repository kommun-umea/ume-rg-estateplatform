using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving a single estate.
/// </summary>
public sealed record EstateDetailsRequest
{
    /// <summary>
    /// Indicates whether buildings should be included with the estate.
    /// </summary>
    [FromQuery(Name = "includeBuildings")]
    [SwaggerParameter("When true, includes ancestor building information for the estate.", Required = false)]
    public bool IncludeBuildings { get; init; }
}
