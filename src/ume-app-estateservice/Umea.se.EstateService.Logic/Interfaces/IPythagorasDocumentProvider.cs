using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IPythagorasDocumentProvider
{
    Task<ICollection<PythagorasDocument>> GetDocumentsAsync();
}
