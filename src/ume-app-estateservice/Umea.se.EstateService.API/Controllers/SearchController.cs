using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Search;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Search)]
[AuthorizeApiKey]
public class SearchController(SearchHandler searchHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ICollection<PythagorasDocument>> Search(AutocompleteRequest req, CancellationToken cancellationToken)
    {
        IReadOnlyList<SearchResult> results = await searchHandler.SearchAsync(
            req.Query,
            req.Type,
            req.Limit,
            req.BuildingId,
            cancellationToken)
            .ConfigureAwait(false);

        return [.. results.Select(r => r.Item)];
    }
}
