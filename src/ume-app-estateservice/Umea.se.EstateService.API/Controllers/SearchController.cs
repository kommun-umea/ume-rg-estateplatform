using Microsoft.AspNetCore.Mvc;
using Umea.se.EstateService.API.Controllers.Requests;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;
using Umea.se.Toolkit.Auth;

namespace Umea.se.EstateService.API.Controllers;

[Produces("application/json")]
[Route(ApiRoutes.Search)]
[AuthorizeApiKey]
public class SearchController(SearchHandler searchHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ICollection<PythagorasDocument>> Search(AutocompleteRequest req)
    {
        IEnumerable<PythagorasDocument> docs = await searchHandler.GetPythagorasDocumentsAsync();

        if (req.Type != AutocompleteType.Any)
        {
            docs = docs.Where(d => d.Type == AutoCompleteTypeToNodeType[req.Type]);
        }

        if (!string.IsNullOrWhiteSpace(req.Query))
        {
            string query = req.Query.Trim();
            docs = docs.Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || (d.PopularName != null && d.PopularName.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (req.Limit > 0)
        {
            docs = docs.Take(req.Limit);
        }

        return [.. docs];
    }

    private readonly Dictionary<AutocompleteType, NodeType> AutoCompleteTypeToNodeType = new()
    {
        { AutocompleteType.Building, NodeType.Building },
        { AutocompleteType.Workspace, NodeType.Room },
    };
}
