using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Buildings)]
[AuthorizeApiKey]
public class BuildingController(IPythagorasHandler pythagorasService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(CancellationToken cancellationToken)
    {
        PythagorasQuery<BuildingInfo> query = new PythagorasQuery<BuildingInfo>()
            .Take(50);

        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasService.GetBuildingsAsync(query, cancellationToken);

        return buildings;
    }

    [HttpGet("search")]
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

    [HttpGet("{buildingId:int}/workspaces")]
    public Task<IReadOnlyList<BuildingWorkspaceModel>> GetBuildingWorkspacesAsync(int buildingId, CancellationToken cancellationToken)
    {
        return pythagorasService.GetBuildingWorkspacesAsync(buildingId, cancellationToken: cancellationToken);
    }
}
