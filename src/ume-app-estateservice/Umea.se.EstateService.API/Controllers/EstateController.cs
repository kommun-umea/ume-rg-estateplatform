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
[Route(ApiRoutes.Estates)]
[AuthorizeApiKey]
public class EstateController(IPythagorasHandler pythagorasService) : ControllerBase
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
        EstateModel? estate = await pythagorasService
            .GetEstateByIdAsync(estateId, request.IncludeBuildings, cancellationToken)
            .ConfigureAwait(false);

        if (estate is null)
        {
            return NotFound();
        }

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
        PythagorasQuery<NavigationFolder> query = BuildQuery(request);

        IReadOnlyList<EstateModel> estates = await pythagorasService
            .GetEstatesAsync(query, cancellationToken);

        return Ok(estates);
    }

    /// <summary>
    /// Gets all buildings for a specific estate.
    /// </summary>
    /// <param name="estateId">The estate ID.</param>
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
        int estateId,
        [FromQuery] PagedQueryRequest request,
        CancellationToken cancellationToken)
    {
        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .ApplyGeneralSearch(request)
            .ApplyPaging(request);

        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasService
            .GetBuildingInfoAsync(query, estateId, cancellationToken);

        return Ok(buildings);
    }

    private static PythagorasQuery<NavigationFolder> BuildQuery(EstateListRequest request)
    {
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .ApplyGeneralSearch(request);

        if (request.IncludeBuildings)
        {
            query = query.WithQueryParameter("includeAscendantBuildings", true);
        }

        return query.ApplyPaging(request);
    }
}
