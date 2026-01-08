using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.BusinessTypes)]
[Authorize]
public class BusinessTypeController(IPythagorasHandler pythagorasHandler) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of business types.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of rooms.</response>
    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, "List of business types", typeof(IReadOnlyList<BusinessTypeModel>))]
    public async Task<ActionResult<IReadOnlyList<BusinessTypeModel>>> GetBusinessTypesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<BusinessTypeModel> businessTypes = await pythagorasHandler.GetBusinessTypesAsync(cancellationToken);

        return Ok(businessTypes);
    }
}
