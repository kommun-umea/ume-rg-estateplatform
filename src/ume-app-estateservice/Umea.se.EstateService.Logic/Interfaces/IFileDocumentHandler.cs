using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IFileDocumentHandler
{
    Task<DocumentFileModel?> GetBuildingDocument(int buildingId, int directoryId, int documentId, CancellationToken cancellationToken = default);
    Task<BuildingDocumentTreeModel> GetBuildingDocumentTree(int buildingId, CancellationToken cancellationToken = default);
    Task<BuildingDocumentTreeNestedModel> GetBuildingDocumentTreeNested(int buildingId, CancellationToken cancellationToken = default);
    Task<int> GetBuildingDocumentCountAsync(int buildingId, CancellationToken cancellationToken = default);
}
