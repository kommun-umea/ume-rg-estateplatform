using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public class SearchHandler(IPythagorasDocumentProvider pythagorasDocumentProvider)
{
    public Task<ICollection<PythagorasDocument>> GetPythagorasDocumentsAsync()
    {
        return pythagorasDocumentProvider.GetDocumentsAsync();
    }
}
