using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.API.Controllers.Responses;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Search)]
[Authorize]
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
        [FromQuery][SwaggerParameter("Search request parameters.", Required = true)] SearchRequest req,
        CancellationToken cancellationToken)
    {
        string? query = req.Query?.Trim();

        SearchFilter filter = new()
        {
            Types = req.Type is { Count: > 0 }
                ? req.Type
                : Array.Empty<AutocompleteType>(),
            BusinessTypeIds = req.BusinessTypeIds
        };

        IReadOnlyList<SearchResult> results = await searchHandler.SearchAsync(
            query,
            filter,
            req.Limit,
            req.GeoFilter,
            cancellationToken)
            .ConfigureAwait(false);

        List<PythagorasDocument> documents = [.. results.Select(result => result.Item)];

        return Ok(documents);
    }

    /// <summary>
    /// Search for building geo locations by query and filters.
    /// </summary>
    /// <remarks>
    /// Returns a list of matching building geo locations based on the provided query and filters.
    /// </remarks>
    /// <param name="req">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns a list of matching geo locations.</response>
    /// <response code="400">If the request parameters are invalid.</response>
    [HttpGet("geolocations")]
    [SwaggerOperation(
        Summary = "Search for geo locations",
        Description = "Search for building geo locations using a query string and filters."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "A list of matching geo locations.", typeof(ICollection<BuildingLocationModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request parameters.")]
    public async Task<ActionResult<ICollection<BuildingLocationModel>>> SearchGeoLocation(
        [FromQuery][SwaggerParameter("Search request parameters.", Required = true)] SearchRequest req,
        CancellationToken cancellationToken)
    {
        string? query = req.Query?.Trim();

        SearchFilter filter = new()
        {
            Types = [AutocompleteType.Building], // Geo locations only exist for buildings
            BusinessTypeIds = req.BusinessTypeIds
        };

        IReadOnlyList<SearchResult> results = await searchHandler.SearchAsync(
            query,
            filter,
            limit: 10000,
            req.GeoFilter,
            cancellationToken);

        List<BuildingLocationModel> locations = results
           .Select(result => new BuildingLocationModel
           {
               Id = result.Item.Id,
               GeoLocation = result.Item.GeoLocation is { } geo
                   ? new GeoPointModel(geo.Lat, geo.Lng)
                   : null
           })
           .ToList();

        return Ok(locations);
    }

#if DEBUG
    /// <summary>
    /// Search with full diagnostic information for debugging search results.
    /// Only available in DEBUG builds.
    /// </summary>
    /// <remarks>
    /// Returns search results along with detailed diagnostic information including:
    /// - Query tokenization and normalization
    /// - Token expansions (exact, prefix, fuzzy, n-gram matches)
    /// - Per-document score breakdown (BM25, bonuses, field hits)
    /// - Applied filters and options
    /// </remarks>
    /// <param name="req">The search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns search results with diagnostic information.</response>
    /// <response code="400">If the request parameters are invalid.</response>
    [HttpGet("debug")]
    [SwaggerOperation(
        Summary = "Search with diagnostics (DEBUG only)",
        Description = "Search for documents with full diagnostic information for debugging. Only available in DEBUG builds."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "Search results with diagnostics.", typeof(SearchDebugResponse))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request parameters.")]
    public async Task<ActionResult<SearchDebugResponse>> SearchDebug(
        [FromQuery][SwaggerParameter("Search request parameters.", Required = true)] SearchRequest req,
        CancellationToken cancellationToken)
    {
        string? query = req.Query?.Trim();

        SearchFilter filter = new()
        {
            Types = req.Type is { Count: > 0 }
                ? req.Type
                : Array.Empty<AutocompleteType>(),
            BusinessTypeIds = req.BusinessTypeIds
        };

        (IReadOnlyList<SearchResult> results, SearchDiagnostics diagnostics) =
            await searchHandler.SearchWithDiagnosticsAsync(
                query,
                filter,
                req.Limit,
                req.GeoFilter,
                cancellationToken)
            .ConfigureAwait(false);

        List<PythagorasDocument> documents = [.. results.Select(result => result.Item)];

        return Ok(new SearchDebugResponse
        {
            Results = documents,
            Diagnostics = diagnostics
        });
    }
#endif
}
