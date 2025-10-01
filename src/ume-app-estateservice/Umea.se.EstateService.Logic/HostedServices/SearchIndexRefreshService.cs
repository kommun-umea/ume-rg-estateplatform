using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.Logic.Options;

namespace Umea.se.EstateService.Logic.HostedServices;

public sealed class SearchIndexRefreshService(
    SearchHandler searchHandler,
    IOptions<SearchIndexRefreshOptions> options,
    ILogger<SearchIndexRefreshService> logger) : BackgroundService
{
    private readonly SearchIndexRefreshOptions _options = options.Value;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Task? _activeRefresh;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = _options.RefreshInterval;
        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning("Search index refresh disabled because interval is {IntervalHours} hour(s).", _options.RefreshIntervalHours);
            return;
        }

        logger.LogInformation("Search index refresh service starting with interval {Interval}.", interval);

        using PeriodicTimer timer = new(interval);

        await RefreshOnceAsync(stoppingToken).ConfigureAwait(false);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RefreshOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // expected during shutdown
        }
        finally
        {
            logger.LogInformation("Search index refresh service stopping.");
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
            logger.LogInformation("Manual search index refresh triggered.");
            return RefreshStatus.Started;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task RefreshOnceAsync(CancellationToken cancellationToken)
    {
        using IDisposable? scope = logger.BeginScope("Search index refresh");
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
            logger.LogError(ex, "Search index refresh failed.");
        }
    }
}

public enum RefreshStatus
{
    Started,
    AlreadyRunning
}
