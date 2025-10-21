﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;
using Umea.se.Toolkit.Auth.Authorization;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Search)]
[Authorize(Policy = ToolkitAuthPolicies.EmployeeOrApiKey)]
public class SearchController(SearchHandler searchHandler) : ControllerBase
{
    /// <summary>
    /// Search for Pythagoras documents (estates, buildings, rooms) by query and types.
    /// </summary>
    /// <remarks>
    /// Returns a list of matching documents based on the provided query, type, and optional filters.
    /// </remarks>
    /// <param name="req">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns a list of matching Pythagoras documents.</response>
    /// <response code="400">If the request parameters are invalid.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Search for Pythagoras documents",
        Description = "Search for estates, buildings, or rooms using a query string and filters."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "A list of matching Pythagoras documents.", typeof(ICollection<PythagorasDocument>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request parameters.")]
    public async Task<ActionResult<ICollection<PythagorasDocument>>> Search(
        [FromQuery][SwaggerParameter("Search request parameters.", Required = true)] AutocompleteRequest req,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AutocompleteType> types = req.Type is { Count: > 0 }
            ? req.Type
            : Array.Empty<AutocompleteType>();

        IReadOnlyList<SearchResult> results = await searchHandler.SearchAsync(
            req.Query,
            types,
            req.Limit,
            cancellationToken)
            .ConfigureAwait(false);

        List<PythagorasDocument> documents = [.. results.Select(result => result.Item)];

        return Ok(documents);
    }
}
