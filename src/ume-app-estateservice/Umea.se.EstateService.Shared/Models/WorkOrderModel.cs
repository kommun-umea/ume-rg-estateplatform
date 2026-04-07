namespace Umea.se.EstateService.Shared.Models;

public class WorkOrderDetailModel
{
    public Guid Id { get; init; }
    public WorkOrderType? WorkOrderType { get; init; }
    public string BuildingName { get; init; } = null!;
    public string? RoomName { get; init; }
    public string Location { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string SyncStatus { get; init; } = null!;
    public string? Status { get; init; }
    public int? PythagorasWorkOrderId { get; init; }
    public string? ErrorMessage { get; init; }
    public int FileCount { get; init; }
    public List<WorkOrderFileModel> Files { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}

public class WorkOrderListItemModel
{
    public Guid Id { get; init; }
    public WorkOrderType? WorkOrderType { get; init; }
    public string BuildingName { get; init; } = null!;
    public string? RoomName { get; init; }
    public string Location { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string SyncStatus { get; init; } = null!;
    public string? Status { get; init; }
    public int FileCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
}

public class WorkOrderSubmissionModel
{
    public Guid Id { get; init; }
    public string SyncStatus { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}

public class WorkOrderFileModel
{
    public string FileName { get; init; } = null!;
    public long FileSize { get; init; }
    public bool Uploaded { get; init; }
}
