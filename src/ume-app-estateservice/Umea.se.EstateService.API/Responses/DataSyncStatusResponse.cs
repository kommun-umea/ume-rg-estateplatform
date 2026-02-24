namespace Umea.se.EstateService.API.Responses;

public sealed class DataSyncStatusResponse
{
    public int DocumentCount { get; init; }
    public DateTime? LastRefreshTime { get; init; }
    public DateTime? NextRefreshTime { get; init; }
    public double RefreshIntervalHours { get; init; }
    public bool IsRefreshing { get; init; }
}
