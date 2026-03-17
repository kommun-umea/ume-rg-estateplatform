using Microsoft.Extensions.Logging;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public interface IWorkOrderStatusSyncService
{
    /// <summary>
    /// Syncs status and notifier info from Pythagoras for submitted work orders that haven't been checked recently.
    /// Uses a single batch call to Pythagoras for all stale orders.
    /// </summary>
    Task SyncStaleWorkOrdersAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default);
}

public class WorkOrderStatusSyncService(
    IWorkOrderRepository workOrderRepository,
    IPythagorasClient pythagorasClient,
    ApplicationConfig appConfig,
    ILogger<WorkOrderStatusSyncService> logger) : IWorkOrderStatusSyncService
{
    private const int BatchSize = 100;
    private readonly WorkOrderConfiguration _config = appConfig.WorkOrderProcessing;

    public async Task SyncStaleWorkOrdersAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<WorkOrderEntity> stale = [.. workOrders
            .Where(wo => wo.SyncStatus == WorkOrderSyncStatus.Submitted
                && wo.PythagorasWorkOrderId is not null
                && wo.NextSyncAt <= now)];

        if (stale.Count == 0)
        {
            return;
        }

        foreach (IEnumerable<WorkOrderEntity> batch in stale.Chunk(BatchSize))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await SyncBatchAsync([.. batch], cancellationToken);
        }
    }

    private async Task SyncBatchAsync(List<WorkOrderEntity> batch, CancellationToken cancellationToken)
    {
        try
        {
            List<int> pythagorasIds = [.. batch.Select(wo => wo.PythagorasWorkOrderId!.Value)];
            IReadOnlyList<WorkOrderDto> pythagorasWorkOrders = await pythagorasClient.GetWorkOrdersByIdsAsync(pythagorasIds, cancellationToken);

            Dictionary<int, WorkOrderDto> lookup = pythagorasWorkOrders.ToDictionary(wo => wo.Id);

            foreach (WorkOrderEntity workOrder in batch)
            {
                if (lookup.TryGetValue(workOrder.PythagorasWorkOrderId!.Value, out WorkOrderDto? pythagoras))
                {
                    if (!string.IsNullOrWhiteSpace(pythagoras.NotifierEmail))
                    {
                        workOrder.NotifierEmail = pythagoras.NotifierEmail.ToLowerInvariant();
                    }

                    if (!string.IsNullOrWhiteSpace(pythagoras.NotifierName))
                    {
                        workOrder.NotifierName = pythagoras.NotifierName;
                    }

                    if (pythagoras.StatusId is not null)
                    {
                        workOrder.PythagorasStatusId = pythagoras.StatusId;
                        workOrder.PythagorasStatusName = pythagoras.StatusName;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync batch of {Count} work orders.", batch.Count);
        }

        // Always push NextSyncAt forward so we don't hammer Pythagoras on repeated failures
        ScheduleNextSync(batch);
        await workOrderRepository.UpdateManyAsync(batch, cancellationToken);
    }

    private void ScheduleNextSync(List<WorkOrderEntity> batch)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset nextSync = now.AddMinutes(_config.StatusCheckIntervalMinutes);

        foreach (WorkOrderEntity workOrder in batch)
        {
            workOrder.NextSyncAt = nextSync;
            workOrder.UpdatedAt = now;
        }
    }
}
