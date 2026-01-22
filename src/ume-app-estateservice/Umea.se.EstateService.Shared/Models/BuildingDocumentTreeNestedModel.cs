namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingDocumentTreeNestedModel
{
    public int TotalDocumentCount { get; init; }
    public int TotalDirectoryCount { get; init; }
    public IReadOnlyList<DocumentTreeNodeModel> Directories { get; init; } = [];
    public IReadOnlyList<DocumentInfoModel> RootDocuments { get; init; } = [];
}
