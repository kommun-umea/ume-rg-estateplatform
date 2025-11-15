namespace Umea.se.EstateService.Logic.Interfaces;

using Umea.se.EstateService.Logic.HostedServices;

/// <summary>
/// Abstraction for orchestrating manual refresh of search-related data and exposing status.
/// Implementations may use different data sources (data store, external API, etc.).
/// </summary>
public interface ISearchRefreshOrchestrator
{
    Task<RefreshStatus> TriggerManualRefreshAsync(CancellationToken cancellationToken = default);

    SearchIndexInfo GetIndexInfo();
}

