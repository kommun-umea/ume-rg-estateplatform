using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Authorization;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Rooms)]
[AuthorizeApiKey]
[Authorize(Policy = AuthPolicies.Employee)]
public class RoomController(IPythagorasHandler pythagorasHandler) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of rooms.
    /// </summary>
    /// <remarks>
    /// Returns a list of rooms filtered by optional parameters.  
    /// - If <paramref name="ids"/> is provided, only rooms with those IDs are returned.  
    /// - If <paramref name="search"/> is provided, rooms matching the search string are returned.  
    /// - If <paramref name="maxResults"/> is provided, limits the number of results.  
    /// You cannot specify both <paramref name="ids"/> and <paramref name="search"/>.
    /// </remarks>
    /// <param name="ids">Optional. Array of room IDs to filter by.</param>
    /// <param name="search">Optional. Search string to filter rooms by name or other properties.</param>
    /// <param name="maxResults">Optional. Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of rooms.</response>
    /// <response code="400">If both ids and search are specified.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get rooms",
        Description = "Retrieves a list of rooms, optionally filtered by IDs, search string, or limited by maxResults."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of rooms", typeof(IReadOnlyList<RoomModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request if both ids and search are specified")]
    public async Task<ActionResult<IReadOnlyList<RoomModel>>> GetRoomsAsync(
        [FromQuery, SwaggerParameter("Array of room IDs to filter by.", Required = false)] int[]? ids,
        [FromQuery, SwaggerParameter("Search string to filter rooms.", Required = false)] string? search,
        [FromQuery, SwaggerParameter("Maximum number of results to return.", Required = false)] int? maxResults,
        CancellationToken cancellationToken)
    {
        if ((ids?.Length ?? 0) > 0 && !string.IsNullOrWhiteSpace(search))
        {
            return BadRequest("Specify either ids or generalSearch, not both.");
        }

        PythagorasQuery<Workspace>? query = null;

        if ((ids?.Length ?? 0) > 0 || !string.IsNullOrWhiteSpace(search) || maxResults is { })
        {
            query = new PythagorasQuery<Workspace>();

            if ((ids?.Length ?? 0) > 0)
            {
                query = query.WithIds(ids!);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.GeneralSearch(search!);
            }

            if (maxResults is int take && take > 0)
            {
                query = query.Take(take);
            }
        }

        IReadOnlyList<RoomModel> rooms = await pythagorasHandler.GetRoomsAsync(query, cancellationToken);
        return Ok(rooms);
    }
}
