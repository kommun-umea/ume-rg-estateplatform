using Umea.se.EstateService.Logic.Handlers.WorkOrder;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Test.TestHelpers;

public class NoOpWorkOrderStatusSyncService : IWorkOrderStatusSyncService
{
    public Task SyncStaleWorkOrdersAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
