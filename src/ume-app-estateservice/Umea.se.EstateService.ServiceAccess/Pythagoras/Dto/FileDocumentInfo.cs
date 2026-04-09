namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class FileDocumentInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? EntityType { get; init; }
    public int? EntityId { get; init; }
    public int? RecordActionTypeId { get; init; }
    public string? RecordActionTypeName { get; init; }
    public int? RecordStatusId { get; init; }
    public string? RecordStatus { get; init; }
    public int? VersionRank { get; init; }
}
