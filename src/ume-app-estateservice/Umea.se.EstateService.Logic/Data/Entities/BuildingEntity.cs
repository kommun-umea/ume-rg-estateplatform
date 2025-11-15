using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Data.Entities;

public class BuildingEntity : BaseEntity
{
    public AddressModel? Address { get; set; }
    public GeoPointModel? GeoLocation { get; set; }
    public decimal GrossArea { get; set; }
    public decimal NetArea { get; set; }
    public string? YearOfConstruction { get; set; }
    public string? ExternalOwner { get; set; }
    public string? PropertyDesignation { get; set; }
    public BuildingNoticeBoardModel? NoticeBoard { get; set; }

    public int NumFloors { get; set; }
    public int NumRooms { get; set; }

    public int EstateId { get; set; }

    public List<FloorEntity> Floors { get; set; } = [];
    public List<RoomEntity> Rooms { get; set; } = [];

    public override int? ParentId => EstateId;
}
