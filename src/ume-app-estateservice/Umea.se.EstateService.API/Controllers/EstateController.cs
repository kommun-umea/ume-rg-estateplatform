using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Estates)]
[Authorize(Policy = AuthPolicy.EstateMunicipalEmployee)]
public class EstateController(IPythagorasHandler pythagorasService) : ControllerBase
{
    /// <summary>
    /// Gets a list of estates.
    /// </summary>
    /// <param name="request">Query parameters for filtering and searching estates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of estates matching the query.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get estates",
        Description = "Retrieves a list of estates. Supports search and filtering via query parameters."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "A list of estates.", typeof(IReadOnlyList<EstateModel>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized.")]
    public async Task<IReadOnlyList<EstateModel>> GetEstatesAsync(
        [FromQuery] EstateRequest request,
        CancellationToken cancellationToken)
    {
        PythagorasQuery<NavigationFolder> query = BuildQuery(request);

        IReadOnlyList<EstateModel> estates = await pythagorasService
            .GetEstatesAsync(query, cancellationToken);

        return estates;
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
        Description = "Retrieves all buildings associated with a specific estate."
    )]
    [SwaggerResponse(StatusCodes.Status200OK, "A list of buildings for the estate.", typeof(IReadOnlyList<BuildingInfoModel>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid estate ID.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Unauthorized.")]
    public async Task<IReadOnlyList<BuildingInfoModel>> GetEstateBuildingsAsync(
        int estateId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasService
            .GetBuildingInfoAsync(null, estateId, cancellationToken);

        return buildings;
    }

    private static PythagorasQuery<NavigationFolder> BuildQuery(EstateRequest request)
    {
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.GeneralSearch(request.SearchTerm);
        }

        if (request.IncludeBuildings)
        {
            query = query.WithQueryParameter("includeAscendantBuildings", true);
        }

        if (request.Limit > 0)
        {
            query = query.Take(50);
        }

        return query;
    }
}
