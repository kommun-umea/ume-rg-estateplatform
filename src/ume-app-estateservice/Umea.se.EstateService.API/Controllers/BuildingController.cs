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
[Route(ApiRoutes.Buildings)]
[AuthorizeApiKey]
public class BuildingController(IPythagorasHandler pythagorasService) : ControllerBase
{
    /// <summary>
    /// Gets a specific building.
    /// </summary>
    /// <param name="buildingId">The building identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested building or 404 when it does not exist.</returns>
    [HttpGet("{buildingId:int}")]
    [SwaggerOperation(
        Summary = "Get building",
        Description = "Retrieves a single building."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "The requested building.", typeof(BuildingInfoModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Building not found.")]
    public async Task<ActionResult<BuildingInfoModel>> GetBuildingAsync(
        int buildingId,
        CancellationToken cancellationToken)
    {
        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .Where(info => info.Id, buildingId);

        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasService
            .GetBuildingsAsync(query, cancellationToken)
            .ConfigureAwait(false);

        BuildingInfoModel? building = buildings.FirstOrDefault();
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
    [SwaggerResponse(StatusCodes.Status200OK, "List of rooms for the building", typeof(IReadOnlyList<BuildingRoomModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid buildingId")]
    public async Task<ActionResult<IReadOnlyList<BuildingRoomModel>>> GetBuildingRoomsAsync(
        int buildingId,
        [FromQuery] BuildingRoomsRequest request,
        CancellationToken cancellationToken)
    {
        PythagorasQuery<BuildingWorkspace> query = new PythagorasQuery<BuildingWorkspace>()
            .ApplyGeneralSearch(request)
            .ApplyPaging(request);

        IReadOnlyList<BuildingRoomModel> rooms = await pythagorasService
            .GetBuildingWorkspacesAsync(buildingId, query, cancellationToken)
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
        PythagorasQuery<Floor> floorQuery = new PythagorasQuery<Floor>()
            .ApplyGeneralSearch(request)
            .ApplyPaging(request);

        IReadOnlyList<FloorInfoModel> floors = request.IncludeRooms
            ? await pythagorasService
                .GetBuildingFloorsWithRoomsAsync(buildingId, floorQuery, cancellationToken: cancellationToken)
                .ConfigureAwait(false)
            : await pythagorasService
                .GetBuildingFloorsAsync(buildingId, floorQuery, cancellationToken)
                .ConfigureAwait(false);

        return Ok(floors);
    }

    private async Task<IReadOnlyList<BuildingInfoModel>> QueryBuildingsAsync(
        BuildingListRequest request,
        CancellationToken cancellationToken)
    {
        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .ApplyGeneralSearch(request)
            .ApplyPaging(request);

        if (request.EstateId is int estateId)
        {
            return await pythagorasService
                .GetBuildingInfoAsync(query, estateId, cancellationToken)
                .ConfigureAwait(false);
        }

        return await pythagorasService
            .GetBuildingsAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
