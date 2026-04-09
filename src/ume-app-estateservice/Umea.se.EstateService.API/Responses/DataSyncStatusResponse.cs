namespace Umea.se.EstateService.API.Responses;

public sealed class DataSyncStatusResponse
{
    public int DocumentCount { get; init; }
    public DateTime? LastRefreshTime { get; init; }
    public DateTime? NextRefreshTime { get; init; }
    public string? RefreshSchedule { get; init; }
    public string? DocumentSyncSchedule { get; init; }
    public string? ImagePreWarmSchedule { get; init; }
    public bool IsRefreshing { get; init; }
}
