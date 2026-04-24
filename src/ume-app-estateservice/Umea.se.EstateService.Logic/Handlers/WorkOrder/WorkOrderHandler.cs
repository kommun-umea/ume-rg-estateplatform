using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.HostedServices;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Exceptions;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderHandler(
    IWorkOrderRepository workOrderRepository,
    IDataStore dataStore,
    WorkOrderChannel workOrderChannel,
    IWorkOrderFileStorage fileStorage,
    IWorkOrderFileValidator fileValidator,
    ILogger<WorkOrderHandler> logger) : IWorkOrderHandler
{
    private static readonly Dictionary<WorkOrderType, PythagorasWorkOrderType> _workOrderTypeMap = new()
    {
        [WorkOrderType.ErrorReport] = PythagorasWorkOrderType.ErrorReport,
        [WorkOrderType.BuildingService] = PythagorasWorkOrderType.BuildingService,
        [WorkOrderType.FacilityService] = PythagorasWorkOrderType.FacilityService,
        [WorkOrderType.TownHallService] = PythagorasWorkOrderType.TownHallService,
    };

    public async Task<WorkOrderSubmissionModel> SubmitWorkOrderAsync(CreateWorkOrderRequest request, string email, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = new();

        if (!_workOrderTypeMap.TryGetValue(request.WorkOrderType, out PythagorasWorkOrderType workOrderType))
        {
            errors.AddError("workOrderType", ValidationErrorCode.InvalidValue);
        }

        bool isErrorReport = request.WorkOrderType == WorkOrderType.ErrorReport;
        WorkOrderLocation? location = null;

        if (isErrorReport)
        {
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                errors.AddError("location", ValidationErrorCode.Required);
            }
            else if (!Enum.TryParse(request.Location, ignoreCase: true, out WorkOrderLocation parsed))
            {
                errors.AddError("location", ValidationErrorCode.InvalidValue);
            }
            else
            {
                location = parsed;
            }
        }

        if (!dataStore.BuildingsById.TryGetValue(request.BuildingId, out BuildingEntity? building))
        {
            errors.AddError("buildingId", ValidationErrorCode.NotFound);
        }
        else if (!building.WorkOrderTypes.Contains(request.WorkOrderType))
        {
            errors.AddError("workOrderType", ValidationErrorCode.NotSupported);
        }

        int? roomId = null;
        string? roomName = null;
        if (isErrorReport && request.RoomId.HasValue)
        {
            if (location == WorkOrderLocation.Outdoor)
            {
                errors.AddError("roomId", ValidationErrorCode.Conflict);
            }
            else if (!dataStore.RoomsById.TryGetValue(request.RoomId.Value, out RoomEntity? room))
            {
                errors.AddError("roomId", ValidationErrorCode.NotFound);
            }
            else if (room.BuildingId != request.BuildingId)
            {
                errors.AddError("roomId", ValidationErrorCode.InvalidValue);
            }
            else
            {
                roomId = room.Id;
                roomName = room.Name;
            }
        }

        errors.ThrowIfErrors();

        WorkOrderEntity workOrder = new()
        {
            Uid = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            BuildingName = building!.Name,
            RoomId = roomId,
            RoomName = roomName,
            Location = location,
            WorkOrderTypeId = (int)workOrderType,
            Description = request.Description,
            SyncStatus = WorkOrderSyncStatus.Pending,
            NextSyncAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByEmail = email.ToLowerInvariant(),
            NotifierEmail = (!string.IsNullOrWhiteSpace(request.NotifierEmail)
                ? request.NotifierEmail
                : email).ToLowerInvariant(),
            NotifierName = request.NotifierName,
            NotifierPhone = request.NotifierPhone
        };

        if (request.Files is { Count: > 0 })
        {
            await fileValidator.ValidateAsync(request.Files, cancellationToken);

            foreach (WorkOrderFileUpload file in request.Files)
            {
                string relativePath = Path.Combine(workOrder.Uid.ToString(), file.FileName);

                await fileStorage.SaveAsync(relativePath, file.Stream, cancellationToken);

                workOrder.Files.Add(new WorkOrderFileEntity
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.FileSize,
                    StoragePath = relativePath,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await workOrderRepository.AddAsync(workOrder, cancellationToken);

        workOrderChannel.Notify(workOrder.Uid);

        logger.LogInformation("WorkOrder {WorkOrderUid} created for building {BuildingId} by {Email}", workOrder.Uid, workOrder.BuildingId, email);

        return WorkOrderMapper.MapToSubmission(workOrder);
    }

    public async Task<IReadOnlyList<WorkOrderListItemModel>> GetWorkOrdersAsync(string email, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkOrderEntity> entities = await workOrderRepository.GetByEmailAsync(email, cancellationToken);
        return WorkOrderMapper.MapToListItems(entities);
    }

    public async Task<WorkOrderDetailModel> GetWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, email, cancellationToken);
        return workOrder is null
            ? throw new EntityNotFoundException($"Work order {uid} not found.")
            : WorkOrderMapper.MapToDetail(workOrder);
    }

    public async Task<WorkOrderDetailModel> SyncWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, email, cancellationToken);
        if (workOrder is null)
        {
            throw new EntityNotFoundException($"Work order {uid} not found.");
        }

        if (workOrder is { SyncStatus: WorkOrderSyncStatus.Submitted, PythagorasWorkOrderId: not null })
        {
            workOrder.NextSyncAt = DateTimeOffset.UtcNow;
            workOrder.UpdatedAt = DateTimeOffset.UtcNow;
            await workOrderRepository.UpdateAsync(workOrder, cancellationToken);
            workOrderChannel.Notify(workOrder.Uid);
        }

        return WorkOrderMapper.MapToDetail(workOrder);
    }

    public async Task<WorkOrderDetailModel> RetryWorkOrderAsync(Guid uid, string email, CancellationToken cancellationToken = default)
    {
        WorkOrderEntity? workOrder = await workOrderRepository.GetByUidAsync(uid, email, cancellationToken);
        if (workOrder is null)
        {
            throw new EntityNotFoundException($"Work order {uid} not found.");
        }

        if (workOrder.SyncStatus is not WorkOrderSyncStatus.Failed || workOrder.NextSyncAt is not null)
        {
            throw new StateConflictException("Work order is not in a permanently failed state.");
        }

        workOrder.SyncStatus = WorkOrderSyncStatus.Pending;
        workOrder.RetryCount = 0;
        workOrder.ErrorMessage = null;
        workOrder.NextSyncAt = DateTimeOffset.UtcNow;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        await workOrderRepository.UpdateAsync(workOrder, cancellationToken);

        workOrderChannel.Notify(workOrder.Uid);

        logger.LogInformation("WorkOrder {WorkOrderUid} manually queued for retry by {Email}.", workOrder.Uid, email);

        return WorkOrderMapper.MapToDetail(workOrder);
    }
}
