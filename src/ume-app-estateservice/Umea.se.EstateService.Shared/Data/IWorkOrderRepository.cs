using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Shared.Data;

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderEntity>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<WorkOrderEntity?> GetByUidAsync(Guid uid, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderEntity>> GetDueForProcessingAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task<bool> TryClaimForProcessingAsync(int id, DateTimeOffset processingTimeout, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken = default);
    Task UpdateManyAsync(IReadOnlyList<WorkOrderEntity> workOrders, CancellationToken cancellationToken = default);
}
