namespace Umea.se.EstateService.Shared.Models;

public sealed class DocumentFileModel
{
    public required string Name { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Content { get; init; }
}
