namespace Umea.se.EstateService.Logic.Data.Entities;

public class RoomEntity : BaseEntity
{
    public double GrossArea { get; set; }
    public double NetArea { get; set; }
    public int Capacity { get; set; }

    public int BuildingId { get; set; }
    public int? FloorId { get; set; }

    public override int? ParentId => BuildingId;
}
