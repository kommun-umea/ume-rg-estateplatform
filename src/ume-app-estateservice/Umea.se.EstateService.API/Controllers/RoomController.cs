using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Rooms)]
[AuthorizeApiKey]
public class RoomController(IPythagorasHandler pythagorasHandler) : ControllerBase
{
    /// <summary>
    /// Retrieves a specific room.
    /// </summary>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="request">Optional parameters controlling expanded data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested room or 404 when it does not exist.</returns>
    [HttpGet("{roomId:int}")]
    [SwaggerOperation(
        Summary = "Get room",
        Description = "Retrieves a single room"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "The requested room.", typeof(RoomDetailsModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Room not found.")]
    public async Task<ActionResult<RoomModel>> GetRoomAsync(int roomId, [FromQuery] RoomDetailsRequest request, CancellationToken cancellationToken)
    {
        PythagorasQuery<Workspace> query = new PythagorasQuery<Workspace>()
            .Where(workspace => workspace.Id, roomId);

        IReadOnlyList<RoomModel> rooms = await pythagorasHandler
            .GetRoomsAsync(query, cancellationToken)
            .ConfigureAwait(false);

        RoomModel? room = rooms.Count > 0 ? rooms[0] : null;
        if (room is null)
        {
            return NotFound();
        }

        return Ok(room);
    }

    /// <summary>
    /// Retrieves a list of rooms.
    /// </summary>
    /// <param name="request">Query parameters for paging, searching, and filtering rooms.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of rooms.</response>
    /// <response code="400">If incompatible filters are combined.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get rooms",
        Description = "Retrieves rooms using the shared limit/offset/search parameters or an explicit id list."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of rooms", typeof(IReadOnlyList<RoomModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request when incompatible filters are provided.")]
    public async Task<ActionResult<IReadOnlyList<RoomModel>>> GetRoomsAsync([FromQuery] RoomListRequest request, CancellationToken cancellationToken)
    {
        PythagorasQuery<Workspace> query = BuildQuery(request);
        IReadOnlyList<RoomModel> rooms = await pythagorasHandler.GetRoomsAsync(query, cancellationToken);

        return Ok(rooms);
    }

    private static PythagorasQuery<Workspace> BuildQuery(RoomListRequest request)
    {
        ImmutableArray<int> ids = request.GetIdsOrEmpty();
        if (ids.Length > 0)
        {
            return new PythagorasQuery<Workspace>().WithIds([.. ids]);
        }

        PythagorasQuery<Workspace> query = new PythagorasQuery<Workspace>()
            .ApplyGeneralSearch(request)
            .ApplyPaging(request);

        if (request.BuildingId is int buildingId)
        {
            query = query.WithQueryParameter("buildingId", buildingId);
        }

        return query;
    }
}
