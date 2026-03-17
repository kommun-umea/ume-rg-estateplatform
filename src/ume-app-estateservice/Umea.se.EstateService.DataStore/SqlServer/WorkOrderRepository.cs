using Microsoft.EntityFrameworkCore;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;

namespace Umea.se.EstateService.DataStore.SqlServer;

public class WorkOrderRepository(EstateDbContext dbContext) : IWorkOrderRepository
{
    public async Task AddAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default)
    {
        dbContext.WorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderEntity>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .Include(e => e.Files)
            .Where(e => e.CreatedByEmail == email || e.NotifierEmail == email)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkOrderEntity?> GetByUidAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .Include(e => e.Files)
            .FirstOrDefaultAsync(e => e.Uid == uid && (e.CreatedByEmail == email || e.NotifierEmail == email), cancellationToken);
    }

    public async Task<IReadOnlyList<WorkOrderEntity>> GetDueForProcessingAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        return await dbContext.WorkOrders
            .Include(e => e.Files)
            .Where(e => e.NextSyncAt != null && e.NextSyncAt <= asOf)
            .Where(e => e.SyncStatus == WorkOrderSyncStatus.Pending
                || e.SyncStatus == WorkOrderSyncStatus.Failed
                || e.SyncStatus == WorkOrderSyncStatus.Submitted
                || e.SyncStatus == WorkOrderSyncStatus.Processing)
            .OrderBy(e => e.NextSyncAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimForProcessingAsync(int id, DateTimeOffset processingTimeout, CancellationToken cancellationToken = default)
    {
        int updated = await dbContext.WorkOrders
            .Where(e => e.Id == id)
            .Where(e => e.SyncStatus == WorkOrderSyncStatus.Pending
                || e.SyncStatus == WorkOrderSyncStatus.Failed
                || (e.SyncStatus == WorkOrderSyncStatus.Processing && e.UpdatedAt < processingTimeout))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.SyncStatus, WorkOrderSyncStatus.Processing)
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
            cancellationToken);
        return updated > 0;
    }

    public async Task UpdateAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default)
    {
        dbContext.WorkOrders.Update(workOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateManyAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default)
    {
        // Entities are already tracked from the query — just save changes
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
