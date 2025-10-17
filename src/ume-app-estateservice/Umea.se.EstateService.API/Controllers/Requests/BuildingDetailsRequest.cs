using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Umea.se.EstateService.API.Controllers.Requests;

/// <summary>
/// Query parameters for retrieving a single building.
/// </summary>
public sealed record BuildingDetailsRequest
{
    /// <summary>
    /// Indicates whether to include the building's estate information.
    /// </summary>
    [FromQuery(Name = "includeEstate")]
    [SwaggerParameter("When true, includes the estate linked to the building (if available).", Required = false)]
    public bool IncludeEstate { get; init; }
}
