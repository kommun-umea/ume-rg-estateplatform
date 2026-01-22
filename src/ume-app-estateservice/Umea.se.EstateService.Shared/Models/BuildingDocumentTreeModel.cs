namespace Umea.se.EstateService.Shared.Models;

public sealed class BuildingDocumentTreeModel
{
    public int TotalDocumentCount { get; init; }
    public int TotalDirectoryCount { get; init; }
    public required IReadOnlyList<DocumentDirectoryInfoModel> Directories { get; init; }
    public required IReadOnlyList<DocumentInfoModel> Documents { get; init; }
}
