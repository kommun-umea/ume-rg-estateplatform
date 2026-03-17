using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.HostedServices;

public sealed class WorkOrderProcessingService(
    IServiceScopeFactory scopeFactory,
    WorkOrderChannel workOrderChannel,
    ApplicationConfig appConfig,
    ILogger<WorkOrderProcessingService> logger) : BackgroundService
{
    private readonly WorkOrderConfiguration _config = appConfig.WorkOrderProcessing;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WorkOrderProcessingService started. Polling every {Interval}s.", _config.ProcessingIntervalSeconds);

        // Process any pending work orders on startup
        await ProcessPendingWorkOrdersAsync(stoppingToken);

        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(_config.ProcessingIntervalSeconds));
            Task timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            Task channelTask = workOrderChannel.Reader.WaitToReadAsync(stoppingToken).AsTask();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for either a channel signal or the periodic timer tick
                await Task.WhenAny(timerTask, channelTask);

                // Drain all channel signals so we don't re-process on stale notifications
                while (workOrderChannel.Reader.TryRead(out _)) { }

                await ProcessPendingWorkOrdersAsync(stoppingToken);

                // Reset whichever completed
                if (timerTask.IsCompleted)
                {
                    timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                }

                if (channelTask.IsCompleted)
                {
                    channelTask = workOrderChannel.Reader.WaitToReadAsync(stoppingToken).AsTask();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("WorkOrderProcessingService stopping.");
        }
    }

    private async Task ProcessPendingWorkOrdersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IWorkOrderProcessor processor = scope.ServiceProvider.GetRequiredService<IWorkOrderProcessor>();
            await processor.ProcessPendingAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in workOrder processing loop.");
        }
    }
}
