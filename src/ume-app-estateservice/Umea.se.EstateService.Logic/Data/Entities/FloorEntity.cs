namespace Umea.se.EstateService.Logic.Data.Entities;

public class FloorEntity : BaseEntity
{
    public double? GrossArea { get; set; }
    public double? NetArea { get; set; }
    public double? Height { get; set; }

    public int BuildingId { get; set; }

    public List<RoomEntity> Rooms { get; set; } = [];

    public override int? ParentId => BuildingId;
}
