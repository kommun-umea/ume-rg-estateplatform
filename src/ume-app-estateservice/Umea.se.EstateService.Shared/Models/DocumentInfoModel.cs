namespace Umea.se.EstateService.Shared.Models;

public sealed class DocumentInfoModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public int? DirectoryId { get; init; }
    public long? SizeInBytes { get; init; }
    public int? ActionTypeId { get; init; }
    public string? ActionTypeName { get; init; }
}
