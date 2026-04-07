using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Cronos;
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
/// Background service that refreshes the data store and search index on a cron schedule.
/// Scheduled and manual triggers are funneled through a single queue and executed sequentially by one worker.
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
        bool loadedFromCache = false;
        DateTimeOffset? lastRefresh = null;

        (DataSnapshot? snapshot, DateTimeOffset? persisted) = await persistence.TryLoadAsync(stoppingToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            loadedFromCache = true;
            lastRefresh = persisted;
            backgroundCache.SeedFrom(snapshot.Buildings);
            dataStore.SetSnapshot(snapshot);

            if (persisted.HasValue)
            {
                dataStore.RecordRefreshAttempt(persisted.Value);
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

        bool hasCron = TryParseCron(out CronExpression? cron, out TimeZoneInfo? tz);

        if (!hasCron && !string.IsNullOrWhiteSpace(_options.Schedule))
        {
            logger.LogError("Invalid cron expression '{Schedule}'. Scheduled refresh disabled.", _options.Schedule);
        }

        if (!loadedFromCache)
        {
            await RunStartupRefreshWithRetriesAsync(stoppingToken).ConfigureAwait(false);
        }
        else if (hasCron && IsOverdue(lastRefresh, cron!, tz!))
        {
            logger.LogInformation("Last refresh was {Ago} ago — running catch-up refresh.",
                DateTimeOffset.UtcNow - lastRefresh);
            await RunStartupRefreshWithRetriesAsync(stoppingToken).ConfigureAwait(false);
        }

        // Start background cache consumer (refreshes stale document counts + image IDs)
        _ = backgroundCache.RunConsumerAsync(stoppingToken);

        if (hasCron)
        {
            _ = RunScheduledRefreshAsync(cron!, tz!, stoppingToken);
        }
        else if (string.IsNullOrWhiteSpace(_options.Schedule))
        {
            logger.LogInformation("Scheduled refresh disabled (no Schedule configured). Manual triggers are still allowed.");
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
            RefreshSchedule = _options.Schedule,
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

    /// <summary>
    /// Sleeps until the next cron occurrence, then triggers a refresh. Repeats until cancelled.
    /// </summary>
    private async Task RunScheduledRefreshAsync(CronExpression cron, TimeZoneInfo tz, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                DateTimeOffset? nextUtc = cron.GetNextOccurrence(utcNow, tz);

                if (nextUtc is null)
                {
                    logger.LogWarning("Cron expression has no future occurrences. Scheduled refresh stopped.");
                    return;
                }

                _nextRefreshTime = nextUtc;
                TimeSpan delay = nextUtc.Value - utcNow;

                logger.LogInformation("Next scheduled refresh at {NextRefresh:u} (in {Delay}).", nextUtc.Value, delay);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

                if (!TryQueueTrigger(RefreshTriggerSource.Scheduled))
                {
                    logger.LogInformation("Skipping scheduled trigger because a refresh is active or queued.");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // normal shutdown path
        }
    }

    private static bool IsOverdue(DateTimeOffset? lastRefresh, CronExpression cron, TimeZoneInfo tz)
    {
        if (!lastRefresh.HasValue)
        {
            return false;
        }

        // If the next occurrence after the last refresh is in the past, we missed a scheduled run.
        DateTimeOffset? nextAfterLast = cron.GetNextOccurrence(lastRefresh.Value, tz);
        return nextAfterLast.HasValue && nextAfterLast.Value <= DateTimeOffset.UtcNow;
    }

    private bool TryParseCron([NotNullWhen(true)] out CronExpression? expression, [NotNullWhen(true)] out TimeZoneInfo? timeZone)
    {
        if (string.IsNullOrWhiteSpace(_options.Schedule))
        {
            expression = null;
            timeZone = null;
            return false;
        }

        try
        {
            expression = CronExpression.Parse(_options.Schedule);
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
            return true;
        }
        catch (Exception ex) when (ex is CronFormatException or TimeZoneNotFoundException)
        {
            expression = null;
            timeZone = null;
            return false;
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
        Scheduled,
        Manual
    }

    private readonly record struct RefreshTrigger(RefreshTriggerSource Source, DateTimeOffset RequestedAtUtc);
}
