/*
using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Autocomplete)]
[AuthorizeApiKey]
public class AutocompleteController(IAutocompleteHandler autocompleteHandler, ILogger<AutocompleteController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AutocompleteResult>> GetAsync([FromQuery] AutocompleteRequest request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Autocomplete request for {Type} (Limit={Limit})", request.Type, request.Limit);

        AutocompleteArgs args = new() { Query = request.Query, Limit = request.Limit, Type = request.Type, BuildingId = request.BuildingId };
        AutocompleteResult response = await autocompleteHandler.SearchAsync(args, cancellationToken);
        return Ok(response);
    }
}

*/
