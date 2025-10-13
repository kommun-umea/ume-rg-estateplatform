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
[Route(ApiRoutes.Buildings)]
[AuthorizeApiKey]
[Authorize(Policy = AuthPolicies.Employee)]
public class BuildingController(IPythagorasHandler pythagorasService) : ControllerBase
{
    /// <summary>
    /// Gets a list of buildings (max 50).
    /// </summary>
    /// <remarks>
    /// Returns a paged list of building information.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of buildings.</response>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all buildings",
        Description = "Returns a list of buildings (max 50)."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of buildings", typeof(IReadOnlyList<BuildingInfoModel>))]
    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(CancellationToken cancellationToken)
    {
        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .Take(50);

        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasService.GetBuildingsAsync(query, cancellationToken);

        return buildings;
    }

    /// <summary>
    /// Searches for buildings containing the specified search term in their name.
    /// </summary>
    /// <param name="searchTerm">The term to search for in building names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of matching buildings.</response>
    [HttpGet("search")]
    [SwaggerOperation(
        Summary = "Search buildings by name",
        Description = "Returns buildings whose name contains the specified search term."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of matching buildings", typeof(IReadOnlyList<BuildingInfoModel>))]
    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsContainingAsync([FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .Contains(b => b.Name, searchTerm);

        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasService.GetBuildingsAsync(query, cancellationToken);

        return buildings;
    }

    /// <summary>
    /// Gets all rooms for a specific building.
    /// </summary>
    /// <param name="buildingId">The ID of the building.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of rooms for the building.</response>
    /// <response code="400">If the buildingId is not valid.</response>
    [HttpGet("{buildingId:int}/rooms")]
    [SwaggerOperation(
        Summary = "Get rooms for a building",
        Description = "Returns all rooms for the specified building."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of rooms for the building", typeof(IReadOnlyList<BuildingRoomModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid buildingId")]
    public Task<IReadOnlyList<BuildingRoomModel>> GetBuildingRoomsAsync(int buildingId, CancellationToken cancellationToken)
    {
        return pythagorasService.GetBuildingWorkspacesAsync(buildingId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets all floors and their rooms for a specific building.
    /// </summary>
    /// <param name="buildingId">The ID of the building.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of floors with their rooms.</response>
    /// <response code="400">If the buildingId is not valid.</response>
    [HttpGet("{buildingId:int}/floors")]
    [SwaggerOperation(
        Summary = "Get floors and rooms for a building",
        Description = "Returns all floors and their rooms for the specified building."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "List of floors with rooms", typeof(IReadOnlyList<FloorWithRoomsModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid buildingId")]
    public async Task<ActionResult<IReadOnlyList<FloorWithRoomsModel>>> GetBuildingFloorsAsync(int buildingId, CancellationToken cancellationToken)
    {
        IReadOnlyList<FloorWithRoomsModel> floors = await pythagorasService
            .GetBuildingFloorsWithRoomsAsync(buildingId, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Ok(floors);
    }
}
