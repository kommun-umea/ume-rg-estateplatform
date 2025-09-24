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
    public async Task<IReadOnlyList<BuildingModel>> GetBuildingsAsync(CancellationToken cancellationToken)
    {
        PythagorasQuery<Building> query = new PythagorasQuery<Building>()
            .Take(50);

        IReadOnlyList<BuildingModel> buildings = await pythagorasService.GetBuildingsAsync(query, cancellationToken);

        return buildings;
    }

    [HttpGet("search")]
    public async Task<IReadOnlyList<BuildingModel>> GetBuildingsContainingAsync([FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        PythagorasQuery<Building> query = new PythagorasQuery<Building>()
            .Contains(b => b.Name, searchTerm);

        IReadOnlyList<BuildingModel> buildings = await pythagorasService.GetBuildingsAsync(query, cancellationToken);

        return buildings;
    }

    [HttpGet("{buildingId:int}/workspaces")]
    public Task<IReadOnlyList<BuildingWorkspaceModel>> GetBuildingWorkspacesAsync(int buildingId, CancellationToken cancellationToken)
    {
        return pythagorasService.GetBuildingWorkspacesAsync(buildingId, cancellationToken: cancellationToken);
    }
}
