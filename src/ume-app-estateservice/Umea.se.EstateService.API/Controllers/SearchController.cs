using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Search;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Search)]
[AuthorizeApiKey]
public class SearchController(SearchHandler searchHandler) : ControllerBase
{
    [HttpGet]
    public async Task<PythagorasDocument> Search()
    {
        ICollection<PythagorasDocument> docs = await searchHandler.GetPythagorasDocumentsAsync();

        return docs.Where(d => d.Type == NodeType.Room).First();
    }
}
