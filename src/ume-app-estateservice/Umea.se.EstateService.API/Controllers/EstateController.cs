using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Extensions;
using Umea.se.EstateService.API.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Handlers.Favorite;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.UserFromToken;
using QueryArgs = Umea.se.EstateService.Logic.Models.QueryArgs;

namespace Umea.se.EstateService.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRoutes.Estates)]
[Authorize]
public class EstateController(IEstateDataQueryHandler pythagorasHandler, IFavoriteHandler favoriteHandler, UserToken userToken) : ControllerBase
{
    /// <summary>
    /// Gets a specific estate.
    /// </summary>
    /// <param name="estateId">The estate identifier.</param>
    /// <param name="request">Query parameters controlling optional expansions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested estate or 404 when it does not exist.</returns>
    [HttpGet("{estateId:int}")]
    [SwaggerOperation(
        Summary = "Get estate",
        Description = "Retrieves a single estate with optional building information."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "The requested estate.", typeof(EstateModel))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Estate not found.")]
    public async Task<ActionResult<EstateModel>> GetEstateAsync(
        int estateId,
        [FromQuery] EstateDetailsRequest request,
        CancellationToken cancellationToken)
    {

        EstateModel estate = await pythagorasHandler.GetEstateByIdAsync(estateId, request.IncludeBuildings, cancellationToken);

        await favoriteHandler.StampFavoriteAsync(userToken.GetRequiredEmail(), estate, cancellationToken);

        return Ok(estate);
    }

    /// <summary>
    /// Gets a list of estates.
    /// </summary>
    /// <param name="request">Query parameters for filtering and searching estates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of estates matching the query.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get estates",
        Description = "Retrieves estates with standard limit/offset paging and optional search filtering."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "A list of estates.", typeof(IReadOnlyList<EstateModel>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized.")]
    public async Task<ActionResult<IReadOnlyList<EstateModel>>> GetEstatesAsync(
        [FromQuery] EstateListRequest request,
        CancellationToken cancellationToken)
    {
        QueryArgs queryArgs = request.ToQueryArgs();

        IReadOnlyList<EstateModel> estates = await pythagorasHandler.GetEstatesWithBuildingsAsync(request.IncludeBuildings, queryArgs, cancellationToken);

        await favoriteHandler.StampFavoritesAsync(userToken.GetRequiredEmail(), estates, cancellationToken);

        return Ok(estates);
    }

    /// <summary>
    /// Gets all buildings for a specific estate.
    /// </summary>
    /// <param name="estateId">The estate ID.</param>
    /// <param name="request">Query parameters for paging and filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of buildings belonging to the estate.</returns>
    [HttpGet("{estateId:int}/buildings")]
    [SwaggerOperation(
        Summary = "Get buildings for an estate",
        Description = "Retrieves buildings for an estate with optional search term and paging parameters."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "A list of buildings for the estate.", typeof(IReadOnlyList<BuildingInfoModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid estate ID.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized.")]
    public async Task<ActionResult<IReadOnlyList<BuildingInfoModel>>> GetEstateBuildingsAsync(
        [Range(1, int.MaxValue, ErrorMessage = "Estate id must be positive.")] int estateId,
        [FromQuery] PagedQueryRequest request,
        CancellationToken cancellationToken)
    {
        QueryArgs queryArgs = request.ToQueryArgs();

        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasHandler
            .GetBuildingsAsync(buildingIds: null, estateId: estateId, queryArgs: queryArgs, cancellationToken: cancellationToken);

        await favoriteHandler.StampFavoritesAsync(userToken.GetRequiredEmail(), buildings, cancellationToken);

        return Ok(buildings);
    }
}
