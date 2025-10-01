using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Estates)]
[AuthorizeApiKey]
public class EstateController(IPythagorasHandler pythagorasService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<EstateModel>> GetEstatesAsync([FromQuery] EstateRequest request, CancellationToken cancellationToken)
    {
        PythagorasQuery<NavigationFolder> query = BuildQuery(request);

        IReadOnlyList<EstateModel> estates = await pythagorasService
            .GetEstatesAsync(query, cancellationToken);

        return estates;
    }

    [HttpGet("{estateId:int}/buildings")]
    public async Task<IReadOnlyList<BuildingInfoModel>> GetEstateBuildingsAsync(int estateId, CancellationToken cancellationToken)
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
