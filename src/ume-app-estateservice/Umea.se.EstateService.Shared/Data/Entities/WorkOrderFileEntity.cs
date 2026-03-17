namespace Umea.se.EstateService.Shared.Data.Entities;

public class WorkOrderFileEntity
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public WorkOrderEntity WorkOrder { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public bool Uploaded { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
