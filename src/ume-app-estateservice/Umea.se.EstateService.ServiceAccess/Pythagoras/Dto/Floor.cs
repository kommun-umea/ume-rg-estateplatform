namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class Floor : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public double? Height { get; init; }
    public double? ReferenceHeight { get; init; }
    public double? GrossFloorarea { get; init; }
}
