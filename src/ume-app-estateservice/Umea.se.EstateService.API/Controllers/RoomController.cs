using System.Collections.Immutable;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Rooms)]
[Authorize]
public class RoomController(IPythagorasHandlerV2 pythagorasHandlerV2) : ControllerBase
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
    [SwaggerResponse(StatusCodes.Status200OK, "The requested room.", typeof(RoomModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Room not found.")]
    public async Task<ActionResult<RoomModel>> GetRoomAsync(int roomId, [FromQuery] RoomDetailsRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<RoomModel> rooms = await pythagorasHandlerV2
            .GetRoomsAsync([roomId], buildingId: null, floorId: null, queryArgs: null, cancellationToken)
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
        ImmutableArray<int> ids = request.GetIdsOrEmpty();
        int[]? roomIds = ids.Length > 0 ? [.. ids] : null;

        QueryArgs? queryArgs = roomIds is null
            ? QueryArgs.Create(
                skip: request.Offset > 0 ? request.Offset : null,
                take: request.Limit > 0 ? request.Limit : null,
                searchTerm: request.SearchTerm)
            : null;

        IReadOnlyList<RoomModel> rooms = await pythagorasHandlerV2
            .GetRoomsAsync(roomIds, request.BuildingId, floorId: null, queryArgs: queryArgs, cancellationToken)
            .ConfigureAwait(false);

        return Ok(rooms);
    }
}
