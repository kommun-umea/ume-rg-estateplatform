using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

internal static class WorkOrderMapper
{
    public static WorkOrderSubmissionModel MapToSubmission(WorkOrderEntity entity) => new()
    {
        Id = entity.Uid,
        SyncStatus = entity.SyncStatus.ToString(),
        CreatedAt = entity.CreatedAt
    };

    public static WorkOrderDetailModel MapToDetail(WorkOrderEntity entity) => new()
    {
        Id = entity.Uid,
        BuildingName = entity.BuildingName,
        RoomName = entity.RoomName,
        Location = entity.Location.ToString(),
        Description = entity.Description,
        SyncStatus = entity.SyncStatus.ToString(),
        Status = entity.PythagorasStatusName,
        PythagorasWorkOrderId = entity.PythagorasWorkOrderId,
        ErrorMessage = entity.ErrorMessage,
        FileCount = entity.Files.Count,
        Files = [.. entity.Files.Select(f => new WorkOrderFileModel
        {
            FileName = f.FileName,
            FileSize = f.FileSize,
            Uploaded = f.Uploaded
        })],
        CreatedAt = entity.CreatedAt,
        SubmittedAt = entity.SubmittedAt
    };

    public static List<WorkOrderListItemModel> MapToListItems(IReadOnlyList<WorkOrderEntity> entities) =>
    [
        .. entities.Select(e => new WorkOrderListItemModel
        {
            Id = e.Uid,
            BuildingName = e.BuildingName,
            RoomName = e.RoomName,
            Location = e.Location.ToString(),
            Description = e.Description.Length > 200 ? e.Description[..200] + "..." : e.Description,
            SyncStatus = e.SyncStatus.ToString(),
            Status = e.PythagorasStatusName,
            FileCount = e.Files.Count,
            CreatedAt = e.CreatedAt,
            SubmittedAt = e.SubmittedAt
        })
    ];
}
