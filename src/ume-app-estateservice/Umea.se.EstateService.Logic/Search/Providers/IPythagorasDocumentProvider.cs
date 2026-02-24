using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Search.Providers;

public interface IPythagorasDocumentProvider
{
    Task<ICollection<PythagorasDocument>> GetDocumentsAsync();
}
