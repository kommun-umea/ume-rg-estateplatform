namespace Umea.se.EstateService.Shared.Models;

public sealed class DocumentTreeNodeModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<DocumentTreeNodeModel> Subdirectories { get; init; } = [];
    public IReadOnlyList<DocumentInfoModel> Documents { get; init; } = [];
}
