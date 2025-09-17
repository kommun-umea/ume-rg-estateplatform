using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Buildings)]
[AuthorizeApiKey]
public class BuildingController(PythagorasService pythagorasService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<Building>> GetBuildingsAsync(CancellationToken cancellationToken)
    {
        // Just a test implementation to verify that everything is wired up correctly.
        // The DTOs return from PythagorasService should be mapped to domain models

        IReadOnlyList<Building> buildings = await pythagorasService.GetBuildingsAsync(query => query.Take(50), cancellationToken);

        return buildings;
    }

    [HttpGet("search")]
    public async Task<IReadOnlyList<Building>> GetBuildingsContainingAsync([FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        IReadOnlyList<Building> buildings = await pythagorasService.GetBuildingsAsync(query => query.Contains(b => b.Name, searchTerm), cancellationToken);

        return buildings;
    }
}
