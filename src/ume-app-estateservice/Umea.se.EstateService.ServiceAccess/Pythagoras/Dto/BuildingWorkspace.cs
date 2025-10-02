namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class BuildingWorkspace : IPythagorasDto
{
    public int Id { get; init; }
    public Guid Uid { get; init; }
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
    public int? FloorId { get; init; }
    public Guid? FloorUid { get; init; }
    public string? FloorName { get; init; }
    public string? FloorPopularName { get; init; }
    public int BuildingId { get; init; }
    public Guid BuildingUid { get; init; }
    public string BuildingName { get; init; } = string.Empty;
    public string? BuildingPopularName { get; init; }
    public string? BuildingOrigin { get; init; }
    public int? StatusId { get; init; }
    public string? StatusName { get; init; }
    public string? StatusColor { get; init; }
    public int? RentalStatusId { get; init; }
    public string? RentalStatusName { get; init; }
    public string? RentalStatusColor { get; init; }
}
