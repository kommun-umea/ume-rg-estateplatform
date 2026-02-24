namespace Umea.se.EstateService.DataStore.Entities;

public class DataSyncMetadata
{
    public int Id { get; set; } = 1;
    public DateTimeOffset LastRefreshUtc { get; set; }
    public int EstateCount { get; set; }
    public int BuildingCount { get; set; }
    public int FloorCount { get; set; }
    public int RoomCount { get; set; }
}
