namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class Workspace
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
    public int Version { get; init; }
    public long Created { get; init; }
    public long Updated { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? PopularName { get; init; }
    public double GrossArea { get; init; }
    public double NetArea { get; init; }
    public bool NotCleanable { get; init; }
    public bool ChangedByErrand { get; init; }
    public double UpliftedArea { get; init; }
    public double CommonArea { get; init; }
    public double Cost { get; init; }
    public double Price { get; init; }
    public int Capacity { get; init; }
    public int OptimalCapacity { get; init; }
}
