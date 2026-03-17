using Umea.se.EstateService.Shared.Data.Enums;

namespace Umea.se.EstateService.Shared.Data.Entities;

public class WorkOrderEntity : BaseEntity
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;

    public int? RoomId { get; set; }
    public string? RoomName { get; set; }

    public WorkOrderLocation Location { get; set; }
    public string Description { get; set; } = string.Empty;
    public WorkOrderSyncStatus SyncStatus { get; set; }
    public int WorkOrderTypeId { get; set; }

    public int? PythagorasWorkOrderId { get; set; }
    public int? CategoryId { get; set; }

    /// <summary>Status ID from Pythagoras. Synced periodically after submission.</summary>
    public int? PythagorasStatusId { get; set; }

    /// <summary>Status name from Pythagoras (e.g. "Registrerad", "Tilldelad").</summary>
    public string? PythagorasStatusName { get; set; }

    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? NextSyncAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
    public string? NotifierEmail { get; set; }
    public string? NotifierName { get; set; }

    public List<WorkOrderFileEntity> Files { get; set; } = [];
}
