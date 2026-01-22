namespace Umea.se.EstateService.Shared.Models;

public sealed class DocumentDirectoryInfoModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public int? ParentId { get; init; }
    public int DocumentCount { get; init; }
    public int DirectoryCount { get; init; }
}
