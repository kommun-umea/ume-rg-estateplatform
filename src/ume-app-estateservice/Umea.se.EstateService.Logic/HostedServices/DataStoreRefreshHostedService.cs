using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.ServiceAccess.Data;

namespace Umea.se.EstateService.Logic.HostedServices;

public sealed class DataStoreRefreshHostedService(
    IDataRefreshService dataRefreshService,
    IDataStore dataStore,
    SearchHandler searchHandler,
    IOptions<DataStoreRefreshOptions> options,
    ILogger<DataStoreRefreshHostedService> logger) : BackgroundService
{
    private readonly DataStoreRefreshOptions _options = options.Value;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Task? _activeRefresh;
    private DateTime? _lastRefreshTime;
    private DateTime? _nextRefreshTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = _options.RefreshInterval;
        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning("Data store refresh disabled because interval is {IntervalHours} hour(s).", _options.RefreshIntervalHours);
            return;
        }

        logger.LogInformation("Data store refresh service starting with interval {Interval}.", interval);

        using PeriodicTimer timer = new(interval);

        bool loadedFromCache = false;
        if (_options.CacheEnabled && dataStore is InMemoryDataStore inMemoryDataStore)
        {
            loadedFromCache = inMemoryDataStore.TryLoadFromJson(_options.CacheFilePath, logger);
            if (loadedFromCache)
            {
                try
                {
                    logger.LogInformation("Rebuilding search index from cached data store snapshot.");
                    await searchHandler.RefreshIndexAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogDebug("Search index refresh from cache cancelled.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Search index refresh from cached data store snapshot failed.");
                }

                DateTimeOffset last = dataStore.LastRefreshUtc ?? DateTimeOffset.UtcNow;
                DateTimeOffset next = last + interval;
                if (next <= DateTimeOffset.UtcNow)
                {
                    next = DateTimeOffset.UtcNow;
                }

                _nextRefreshTime = next.UtcDateTime;
            }
        }

        if (!loadedFromCache || _options.AlwaysRefreshOnStartup)
        {
            await RefreshOnceAsync(stoppingToken).ConfigureAwait(false);
            _nextRefreshTime = DateTime.UtcNow.Add(interval);
        }

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RefreshOnceAsync(stoppingToken).ConfigureAwait(false);
                _nextRefreshTime = DateTime.UtcNow.Add(interval);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // expected during shutdown
        }
        finally
        {
            logger.LogInformation("Data store refresh service stopping.");
        }
    }

    public async Task<RefreshStatus> TriggerManualRefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0).ConfigureAwait(false))
        {
            return RefreshStatus.AlreadyRunning;
        }

        try
        {
            if (_activeRefresh is { IsCompleted: false })
            {
                return RefreshStatus.AlreadyRunning;
            }

            _activeRefresh = Task.Run(() => RefreshOnceAsync(CancellationToken.None));
            logger.LogInformation("Manual data store refresh triggered.");
            return RefreshStatus.Started;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public DataStoreInfo GetDataStoreInfo()
    {
        return new DataStoreInfo
        {
            EstateCount = dataStore.Estates.Count(),
            BuildingCount = dataStore.Buildings.Count(),
            FloorCount = dataStore.Floors.Count(),
            RoomCount = dataStore.Rooms.Count(),
            IsReady = dataStore.IsReady,
            LastRefreshTime = dataStore.LastRefreshUtc,
            LastAttemptTime = dataStore.LastAttemptUtc,
            NextRefreshTime = _nextRefreshTime,
            RefreshIntervalHours = _options.RefreshIntervalHours,
            IsRefreshing = _activeRefresh is { IsCompleted: false }
        };
    }

    private async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        using IDisposable? scope = logger.BeginScope("Data store + search index refresh");
        try
        {
            logger.LogInformation("Refreshing data store.");
            await dataRefreshService.RefreshDataAsync(cancellationToken).ConfigureAwait(false);
            _lastRefreshTime = DateTime.UtcNow;
            logger.LogInformation("Data store refresh completed successfully.");

            try
            {
                logger.LogInformation("Refreshing search index.");
                await searchHandler.RefreshIndexAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Search index refresh completed successfully.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("Search index refresh cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search index refresh failed after successful data store refresh.");
            }

            if (_options.CacheEnabled && dataStore is InMemoryDataStore inMemoryDataStore)
            {
                inMemoryDataStore.TrySaveToJson(_options.CacheFilePath, logger);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("Data store refresh cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data store refresh failed. Search index will not be refreshed in this cycle.");

            if (!dataStore.IsReady && dataStore is InMemoryDataStore inMemoryDataStore)
            {
                inMemoryDataStore.SignalInitialRefreshFailed(ex);
            }
        }
    }
}
