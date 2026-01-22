namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class FileDocument : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public string Name { get; init; } = string.Empty;
    public long DataSize { get; init; }
    public string? Type { get; init; }
    public long Updated { get; init; }
    public long Created { get; init; }
    public string? Username { get; init; }
    public int? ActionTypeId { get; init; }
    public string? ActionTypeName { get; init; }

}
