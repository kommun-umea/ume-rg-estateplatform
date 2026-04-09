using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Sync;

/// <summary>
/// Owns all data pipeline logic: restoring from cache on startup, running the core refresh
/// (fetch → search index → persist), and post-refresh supplementary syncs.
/// <see cref="DataSyncService"/> handles scheduling and triggers only.
/// </summary>
public sealed class RefreshPipelineRunner(
    IDataRefreshService dataRefreshService,
    IDataStore dataStore,
    IDataStorePersistence persistence,
    IDbContextFactory<EstateDbContext> dbContextFactory,
    SearchHandler searchHandler,
    ILogger<RefreshPipelineRunner> logger)
{
    // ── Startup restore ──────────────────────────────────────────────

    public async Task<(bool LoadedFromCache, DateTimeOffset? LastRefresh)> TryRestoreFromCacheAsync(CancellationToken ct)
    {
        (DataSnapshot? snapshot, DateTimeOffset? persisted) = await persistence.TryLoadAsync(ct).ConfigureAwait(false);

        if (snapshot is null)
        {
            return (false, null);
        }

        dataStore.SetSnapshot(snapshot);

        if (persisted.HasValue)
        {
            dataStore.RecordRefreshAttempt(persisted.Value);
        }

        await StampDocumentCountsAsync(ct).ConfigureAwait(false);

        try
        {
            await searchHandler.RefreshIndexAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Search refresh from cache failed.");
        }

        return (true, persisted);
    }

    // ── Core refresh pipeline ────────────────────────────────────────

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dataRefreshService.RefreshDataAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await searchHandler.RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Search refresh failed after data refresh.");
            }

            await StampDocumentCountsAsync(cancellationToken).ConfigureAwait(false);

            DataSnapshot snapshot = dataStore.GetCurrentSnapshot();
            DateTimeOffset refreshTime = dataStore.LastRefreshUtc ?? DateTimeOffset.UtcNow;
            await persistence.SaveAsync(snapshot, refreshTime, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Refresh cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refresh failed.");
        }
    }

    // ── Document count stamping ───────────────────────────────────────

    private async Task StampDocumentCountsAsync(CancellationToken ct)
    {
        try
        {
            await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

            Dictionary<int, int> counts = await context.BuildingDocuments
                .GroupBy(d => d.BuildingId)
                .Select(g => new { BuildingId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BuildingId, x => x.Count, ct)
                .ConfigureAwait(false);

            foreach (BuildingEntity building in dataStore.Buildings)
            {
                building.NumDocuments = counts.TryGetValue(building.Id, out int count) ? count : 0;
            }

            logger.LogInformation("Stamped document counts on {Count} buildings.", counts.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to stamp document counts — NumDocuments will be null until next sync.");
        }
    }

}
