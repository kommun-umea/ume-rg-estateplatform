using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IIndexedPythagorasDocumentReader
{
    Task<IReadOnlyCollection<PythagorasDocument>> GetIndexedDocumentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, PythagorasDocument>> GetBuildingDocumentsByIdsAsync(IEnumerable<int> buildingIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PythagorasDocument>> GetBuildingsForEstateAsync(int estateId, CancellationToken cancellationToken = default);
}
