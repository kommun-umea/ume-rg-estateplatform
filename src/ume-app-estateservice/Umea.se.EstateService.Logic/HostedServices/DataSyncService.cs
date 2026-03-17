using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.HostedServices;

/// <summary>
/// Background service that refreshes the data store and search index on a periodic schedule.
/// Periodic and manual triggers are funneled through a single queue and executed sequentially by one worker.
/// </summary>
public sealed class DataSyncService(
    IDataRefreshService dataRefreshService,
    IDataStore dataStore,
    IDataStorePersistence persistence,
    BuildingBackgroundCache backgroundCache,
    SearchHandler searchHandler,
    ApplicationConfig appConfig,
    ILogger<DataSyncService> logger) : BackgroundService
{
    private readonly DataSyncConfiguration _options = appConfig.DataSync;
    private readonly Channel<RefreshTrigger> _triggerQueue =
        Channel.CreateUnbounded<RefreshTrigger>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    private int _isRefreshing;
    private int _hasPendingTrigger;
    private DateTimeOffset? _nextRefreshTime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = _options.RefreshInterval;
        bool loadedFromCache = false;

        (DataSnapshot? snapshot, DateTimeOffset? lastRefresh) = await persistence.TryLoadAsync(stoppingToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            loadedFromCache = true;
            backgroundCache.SeedFrom(snapshot.Buildings);
            dataStore.SetSnapshot(snapshot);

            if (lastRefresh.HasValue)
            {
                dataStore.RecordRefreshAttempt(lastRefresh.Value);
            }

            try
            {
                await searchHandler.RefreshIndexAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Search refresh from cache failed.");
            }
        }

        if (!loadedFromCache || _options.AlwaysRefreshOnStartup)
        {
            await RunStartupRefreshWithRetriesAsync(stoppingToken).ConfigureAwait(false);
        }

        // Start background cache consumer (refreshes stale document counts + image IDs)
        _ = backgroundCache.RunConsumerAsync(stoppingToken);

        if (interval > TimeSpan.Zero)
        {
            _nextRefreshTime = DateTimeOffset.UtcNow.Add(interval);
            _ = RunPeriodicTriggerProducerAsync(interval, stoppingToken);
        }
        else
        {
            logger.LogInformation("Periodic refresh disabled (interval <= 0). Manual triggers are still allowed.");
        }

        try
        {
            while (await _triggerQueue.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    while (_triggerQueue.Reader.TryRead(out RefreshTrigger trigger))
                    {
                        await RunRefreshOwnedAsync(trigger.Source, stoppingToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    // Reset only after all queued triggers are drained, so IsBusy()
                    // and TryQueueTrigger remain accurate while work is in progress.
                    Interlocked.Exchange(ref _hasPendingTrigger, 0);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("DataSyncService stopping.");
        }
    }

    public Task<RefreshStatus> TriggerManualRefreshAsync()
    {
        if (IsBusy())
        {
            return Task.FromResult(RefreshStatus.AlreadyRunning);
        }

        return Task.FromResult(TryQueueTrigger(RefreshTriggerSource.Manual)
            ? RefreshStatus.Started
            : RefreshStatus.AlreadyRunning);
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
            IsRefreshing = IsBusy()
        };
    }

    private async Task RunStartupRefreshWithRetriesAsync(CancellationToken cancellationToken)
    {
        int maxAttempts = _options.MaxRetries + 1;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt > 1)
            {
                int delaySeconds = _options.RetryBaseDelaySeconds * (1 << (attempt - 2));
                delaySeconds = Math.Min(delaySeconds, 300);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
            }

            await RunRefreshOwnedAsync(RefreshTriggerSource.Startup, cancellationToken).ConfigureAwait(false);

            if (dataStore.IsReady)
            {
                return;
            }
        }

        logger.LogCritical("Startup refresh failed after {Attempts} attempts.", maxAttempts);
    }

    private async Task RunPeriodicTriggerProducerAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!TryQueueTrigger(RefreshTriggerSource.Periodic))
                {
                    logger.LogInformation("Skipping periodic trigger because a refresh is active or queued.");
                }

                _nextRefreshTime = DateTimeOffset.UtcNow.Add(interval);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // normal shutdown path
        }
    }

    private bool TryQueueTrigger(RefreshTriggerSource source)
    {
        if (Interlocked.CompareExchange(ref _hasPendingTrigger, 1, 0) != 0)
        {
            return false;
        }

        if (!_triggerQueue.Writer.TryWrite(new RefreshTrigger(source, DateTimeOffset.UtcNow)))
        {
            Interlocked.Exchange(ref _hasPendingTrigger, 0);
            return false;
        }

        return true;
    }

    private async Task RunRefreshOwnedAsync(RefreshTriggerSource source, CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _isRefreshing, 1);

        try
        {
            await RunRefreshPipelineAsync(source, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _isRefreshing, 0);
        }
    }

    private async Task RunRefreshPipelineAsync(RefreshTriggerSource source, CancellationToken cancellationToken)
    {
        using IDisposable? scope = logger.BeginScope("Refresh source: {Source}", source);

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

            DataSnapshot snapshot = dataStore.GetCurrentSnapshot();

            // Persistence hydration: stamp cached values onto freshly-created snapshot entities
            // before saving. The live entities are already up-to-date via write-through, but the
            // snapshot may contain new entity instances from the refresh that haven't been touched yet.
            backgroundCache.ApplyTo(snapshot.Buildings);
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

    private bool IsBusy()
        => Volatile.Read(ref _isRefreshing) == 1 || Volatile.Read(ref _hasPendingTrigger) == 1;

    private enum RefreshTriggerSource
    {
        Startup,
        Periodic,
        Manual
    }

    private readonly record struct RefreshTrigger(RefreshTriggerSource Source, DateTimeOffset RequestedAtUtc);
}
