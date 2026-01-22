namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class FileDocumentDirectory : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public int NumberOfChildFiles { get; init; }
    public int NumberOfChildFolders { get; init; }
    public int NumberOfSecuredChildFiles { get; init; }
    public string? Path { get; init; }
    public string? Type { get; init; }
    public long Updated { get; init; }
    public long Created { get; init; }
    public string? Username { get; init; }
    public int? ParentId { get; init; }
    public int EntityId { get; init; }
    public string? EntityType { get; init; }

}
