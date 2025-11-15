using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Interfaces;

namespace Umea.se.EstateService.Logic.HostedServices;

/// <summary>
/// Search refresh orchestrator backed by the data store refresh hosted service.
/// </summary>
public sealed class DataStoreSearchRefreshOrchestrator(
    DataStoreRefreshHostedService dataStoreRefreshHostedService,
    SearchHandler searchHandler) : ISearchRefreshOrchestrator
{
    public Task<RefreshStatus> TriggerManualRefreshAsync(CancellationToken cancellationToken = default)
    {
        // DataStoreRefreshHostedService already orchestrates data + index refresh.
        return dataStoreRefreshHostedService.TriggerManualRefreshAsync();
    }

    public SearchIndexInfo GetIndexInfo()
    {
        DataStoreInfo dataInfo = dataStoreRefreshHostedService.GetDataStoreInfo();

        return new SearchIndexInfo
        {
            DocumentCount = searchHandler.GetDocumentCount(),
            LastRefreshTime = dataInfo.LastRefreshTime?.UtcDateTime,
            NextRefreshTime = dataInfo.NextRefreshTime,
            RefreshIntervalHours = dataInfo.RefreshIntervalHours,
            IsRefreshing = dataInfo.IsRefreshing
        };
    }
}

