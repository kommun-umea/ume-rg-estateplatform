namespace Umea.se.EstateService.Logic.Models;

public sealed class DataStoreInfo
{
    public int EstateCount { get; init; }
    public int BuildingCount { get; init; }
    public int FloorCount { get; init; }
    public int RoomCount { get; init; }
    public bool IsReady { get; init; }
    public DateTimeOffset? LastRefreshTime { get; init; }
    public DateTimeOffset? LastAttemptTime { get; init; }
    public DateTimeOffset? NextRefreshTime { get; init; }
    public double RefreshIntervalHours { get; init; }
    public bool IsRefreshing { get; init; }
}
