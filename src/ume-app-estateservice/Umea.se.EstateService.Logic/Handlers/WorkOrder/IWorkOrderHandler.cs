using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public interface IWorkOrderHandler
{
    Task<WorkOrderSubmissionModel> SubmitWorkOrderAsync(CreateWorkOrderRequest request, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderListItemModel>> GetWorkOrdersAsync(string email, CancellationToken cancellationToken = default);
    Task<WorkOrderDetailModel> GetWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default);
    Task<WorkOrderDetailModel> SyncWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default);
    Task<WorkOrderDetailModel> RetryWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default);
}

public class CreateWorkOrderRequest
{
    public int BuildingId { get; init; }
    public WorkOrderType WorkOrderType { get; init; }
    public string? Location { get; init; }
    public int? RoomId { get; init; }
    public string Description { get; init; } = null!;
    public string? NotifierEmail { get; init; }
    public string? NotifierName { get; init; }
    public string? NotifierPhone { get; init; }
    public List<WorkOrderFileUpload>? Files { get; init; }
}

public class WorkOrderFileUpload
{
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long FileSize { get; init; }
    public Stream Stream { get; init; } = null!;
}
