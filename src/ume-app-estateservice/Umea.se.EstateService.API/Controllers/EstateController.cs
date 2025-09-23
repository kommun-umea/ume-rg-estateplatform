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
            .GetEstatesAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return estates;
    }

    private static PythagorasQuery<NavigationFolder> BuildQuery(EstateRequest request)
    {
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .Where(f => f.TypeId, NavigationFolderType.Estate);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query.GeneralSearch(request.SearchTerm);
        }

        return query;
    }
}
