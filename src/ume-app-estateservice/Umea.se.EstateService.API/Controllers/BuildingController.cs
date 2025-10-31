using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;
using QueryArgs = Umea.se.EstateService.Logic.Interfaces.QueryArgs;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Buildings)]
[AuthorizeApiKey]
public class BuildingController(IPythagorasHandler pythagorasService) : ControllerBase
{
    /// <summary>
    /// Gets details for a specific building.
    /// </summary>
    /// <param name="buildingId">The ID of the building.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the requested building.</response>
    /// <response code="400">If the buildingId is not valid.</response>
    /// <response code="404">If the building does not exist.</response>
    [HttpGet("{buildingId:int}")]
    [SwaggerOperation(
        Summary = "Get a building",
        Description = "Retrieves a single building including its extended properties."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "The building", typeof(BuildingInfoModel))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid buildingId")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Building not found")]
    public async Task<ActionResult<BuildingInfoModel>> GetBuildingByIdAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            return BadRequest("Building id must be positive.");
        }

        BuildingInfoModel? building = await pythagorasService
            .GetBuildingByIdAsync(buildingId, BuildingIncludeOptions.ExtendedProperties | BuildingIncludeOptions.Ascendants, cancellationToken)
            .ConfigureAwait(false);

        if (building is null)
        {
            return NotFound();
        }

        return Ok(building);
    }

    /// <summary>
    /// Gets a list of buildings.
    /// </summary>
    /// <remarks>
    /// Returns building information using the standard limit/offset paging model.
    /// </remarks>
    /// <param name="request">Query parameters for paging and filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of buildings.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get buildings",
        Description = "Retrieves buildings using limit/offset paging, search, and optional estate filtering."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of buildings", typeof(IReadOnlyList<BuildingInfoModel>))]
    public async Task<ActionResult<IReadOnlyList<BuildingInfoModel>>> GetBuildingsAsync(
        [FromQuery] BuildingListRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BuildingInfoModel> buildings = await QueryBuildingsAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(buildings);
    }

    /// <summary>
    /// Gets rooms for a specific building.
    /// </summary>
    /// <param name="buildingId">The ID of the building.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of rooms for the building.</response>
    /// <response code="400">If the buildingId is not valid.</response>
    [HttpGet("{buildingId:int}/rooms")]
    [SwaggerOperation(
        Summary = "Get rooms for a building",
        Description = "Retrieves rooms for the specified building using the shared query parameters."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of rooms for the building", typeof(IReadOnlyList<RoomModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid buildingId")]
    public async Task<ActionResult<IReadOnlyList<RoomModel>>> GetBuildingRoomsAsync(
        int buildingId,
        [FromQuery] BuildingRoomsRequest request,
        CancellationToken cancellationToken)
    {
        QueryArgs queryArgs = QueryArgs.Create(
            skip: request.Offset > 0 ? request.Offset : null,
            take: request.Limit > 0 ? request.Limit : null,
            searchTerm: request.SearchTerm);

        IReadOnlyList<RoomModel> rooms = await pythagorasService
            .GetBuildingWorkspacesAsync(buildingId, request.FloorId, queryArgs, cancellationToken)
            .ConfigureAwait(false);

        return Ok(rooms);
    }

    /// <summary>
    /// Gets floors for a specific building, optionally including their rooms.
    /// </summary>
    /// <param name="buildingId">The ID of the building.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of floors with their rooms.</response>
    /// <response code="400">If the buildingId is not valid.</response>
    [HttpGet("{buildingId:int}/floors")]
    [SwaggerOperation(
        Summary = "Get floors for a building",
        Description = "Retrieves floors for the specified building with standard paging/search parameters. Room data is included when includeRooms=true"
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of floors. Rooms collection populated only when includeRooms=true", typeof(IReadOnlyList<FloorInfoModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid buildingId")]
    public async Task<ActionResult<IReadOnlyList<FloorInfoModel>>> GetBuildingFloorsAsync(
        int buildingId,
        [FromQuery] BuildingFloorsRequest request,
        CancellationToken cancellationToken)
    {
        QueryArgs queryArgs = QueryArgs.Create(
            skip: request.Offset > 0 ? request.Offset : null,
            take: request.Limit > 0 ? request.Limit : null,
            searchTerm: request.SearchTerm);

        IReadOnlyList<FloorInfoModel> floors = await pythagorasService
            .GetBuildingFloorsAsync(buildingId, request.IncludeRooms, floorsQueryArgs: queryArgs, roomsQueryArgs: null, cancellationToken)
            .ConfigureAwait(false);

        return Ok(floors);
    }

    private async Task<IReadOnlyList<BuildingInfoModel>> QueryBuildingsAsync(
        BuildingListRequest request,
        CancellationToken cancellationToken)
    {
        QueryArgs queryArgs = QueryArgs.Create(
            skip: request.Offset > 0 ? request.Offset : null,
            take: request.Limit > 0 ? request.Limit : null,
            searchTerm: request.SearchTerm);

        return await pythagorasService.GetBuildingsAsync(buildingIds: null, estateId: request.EstateId, includeOptions: BuildingIncludeOptions.None, queryArgs: queryArgs, cancellationToken).ConfigureAwait(false);
    }
}
