namespace Umea.se.EstateService.Logic.HostedServices;

public sealed class SearchIndexInfo
{
    public int DocumentCount { get; init; }
    public DateTime? LastRefreshTime { get; init; }
    public DateTime? NextRefreshTime { get; init; }
    public double RefreshIntervalHours { get; init; }
    public bool IsRefreshing { get; init; }
}
