using Microsoft.Extensions.Logging;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Data.Enums;
using Umea.se.EstateService.Shared.Infrastructure;
using Umea.se.EstateService.Shared.Infrastructure.ConfigurationModels;

namespace Umea.se.EstateService.Logic.Handlers.WorkOrder;

public class WorkOrderProcessor(
    IWorkOrderRepository workOrderRepository,
    IPythagorasClient pythagorasClient,
    IWorkOrderStatusSyncService statusSyncService,
    IWorkOrderCategoryClassifier categoryClassifier,
    IWorkOrderFileStorage fileStorage,
    ApplicationConfig appConfig,
    ILogger<WorkOrderProcessor> logger) : IWorkOrderProcessor
{
    private readonly WorkOrderConfiguration _config = appConfig.WorkOrderProcessing;

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<WorkOrderEntity> workOrders = await workOrderRepository
                .GetDueForProcessingAsync(DateTimeOffset.UtcNow, cancellationToken);

            if (workOrders.Count == 0)
            {
                return;
            }

            logger.LogInformation("Processing {Count} workOrders.", workOrders.Count);

            // Batch sync all submitted orders in a single Pythagoras call
            List<WorkOrderEntity> submitted = [.. workOrders.Where(wo => wo.SyncStatus == WorkOrderSyncStatus.Submitted)];
            if (submitted.Count > 0)
            {
                await statusSyncService.SyncStaleWorkOrdersAsync(submitted, cancellationToken);
            }

            // Submit pending/failed/stale-processing orders individually
            foreach (WorkOrderEntity workOrder in workOrders.Where(wo => wo.SyncStatus != WorkOrderSyncStatus.Submitted))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await SubmitWorkOrderAsync(workOrder, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in workOrder processing loop.");
        }
    }

    private async Task SubmitWorkOrderAsync(WorkOrderEntity workOrder, CancellationToken cancellationToken)
    {
        try
        {
            if (!await TryClaimAsync(workOrder, cancellationToken))
            {
                return;
            }

            if (workOrder.PythagorasWorkOrderId is null)
            {
                await ClassifyBestEffortAsync(workOrder, cancellationToken);
                await CreateInPythagorasAsync(workOrder, cancellationToken);
            }

            await UploadRemainingFilesAsync(workOrder, cancellationToken);
            await MarkSubmittedAsync(workOrder, cancellationToken);
            await CleanupFilesAsync(workOrder);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Reset status so the order is retried on next startup
            workOrder.SyncStatus = WorkOrderSyncStatus.Pending;
            workOrder.UpdatedAt = DateTimeOffset.UtcNow;
            await workOrderRepository.UpdateAsync(workOrder, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit workOrder {WorkOrderUid}. Attempt {Attempt}.", workOrder.Uid, workOrder.RetryCount + 1);

            workOrder.RetryCount++;
            workOrder.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            workOrder.UpdatedAt = DateTimeOffset.UtcNow;

            if (workOrder.RetryCount >= _config.MaxRetries)
            {
                workOrder.SyncStatus = WorkOrderSyncStatus.Failed;
                workOrder.NextSyncAt = null;
                logger.LogError("WorkOrder {WorkOrderUid} permanently failed after {MaxRetries} retries.", workOrder.Uid, _config.MaxRetries);
            }
            else
            {
                workOrder.SyncStatus = WorkOrderSyncStatus.Failed;
                double delaySeconds = _config.RetryBaseDelaySeconds * Math.Pow(2, workOrder.RetryCount - 1);
                workOrder.NextSyncAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
                logger.LogInformation("WorkOrder {WorkOrderUid} scheduled for retry at {NextSync}.", workOrder.Uid, workOrder.NextSyncAt);
            }

            await workOrderRepository.UpdateAsync(workOrder, cancellationToken);
        }
    }

    private async Task<bool> TryClaimAsync(WorkOrderEntity workOrder, CancellationToken ct)
    {
        DateTimeOffset processingTimeout = DateTimeOffset.UtcNow.AddMinutes(-_config.ProcessingTimeoutMinutes);
        if (!await workOrderRepository.TryClaimForProcessingAsync(workOrder.Id, processingTimeout, ct))
        {
            logger.LogDebug("Work order {WorkOrderUid} already claimed by another instance, skipping.", workOrder.Uid);
            return false;
        }
        workOrder.SyncStatus = WorkOrderSyncStatus.Processing;
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    private async Task ClassifyBestEffortAsync(WorkOrderEntity workOrder, CancellationToken ct)
    {
        if (workOrder.CategoryId is not null)
        {
            return;
        }

        try
        {
            IReadOnlyList<WorkOrderCategorySuggestion> suggestions = await categoryClassifier.ClassifyAsync(
                workOrder.Description, workOrder.WorkOrderTypeId, ct);

            if (suggestions.Count > 0 && suggestions[0].Confidence >= 0.75)
            {
                workOrder.CategoryId = suggestions[0].CategoryId;
                await workOrderRepository.UpdateAsync(workOrder, ct);
                logger.LogInformation(
                    "Work order {WorkOrderUid} classified as category {CategoryId} ({CategoryName}) with confidence {Confidence:F2}.",
                    workOrder.Uid, suggestions[0].CategoryId, suggestions[0].CategoryName, suggestions[0].Confidence);
            }
            else if (suggestions.Count > 0)
            {
                logger.LogInformation(
                    "Work order {WorkOrderUid} best category match {CategoryName} had insufficient confidence {Confidence:F2} (threshold: 0.75). Submitting without category.",
                    workOrder.Uid, suggestions[0].CategoryName, suggestions[0].Confidence);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Category classification failed for work order {WorkOrderUid}. Submitting without category.", workOrder.Uid);
        }
    }

    private async Task CreateInPythagorasAsync(WorkOrderEntity workOrder, CancellationToken ct)
    {
        CreatePythagorasWorkOrderRequest createRequest = new()
        {
            Description = workOrder.Description,
            BoundObjectType = workOrder.RoomId.HasValue
                ? WorkOrderBoundObjectType.WORKSPACE
                : WorkOrderBoundObjectType.BUILDING,
            BoundObjectIds = [workOrder.RoomId ?? workOrder.BuildingId],
            NotifierEmail = workOrder.NotifierEmail ?? workOrder.CreatedByEmail,
            NotifierName = workOrder.NotifierName,
            NotifierTelephone = workOrder.NotifierPhone,
            NotifierUsername = workOrder.NotifierEmail ?? workOrder.CreatedByEmail,
            CategoryId = workOrder.CategoryId,
        };

        WorkOrderDto? created = await pythagorasClient.CreateWorkOrderAsync(
            (PythagorasWorkOrderType)workOrder.WorkOrderTypeId, PythagorasWorkOrderOrigin.PYTHAGORAS_WEB, createRequest, ct) ?? throw new InvalidOperationException("Pythagoras returned null when creating workOrder.");
        workOrder.PythagorasWorkOrderId = created.Id;

        await workOrderRepository.UpdateAsync(workOrder, ct);
    }

    private async Task UploadRemainingFilesAsync(WorkOrderEntity workOrder, CancellationToken ct)
    {
        int workOrderId = workOrder.PythagorasWorkOrderId!.Value;

        int? parentId = await DiscoverWorkOrderFolderAsync(workOrderId, ct);
        int? actionTypeId = _config.DocumentActionTypeId;
        int? actionTypeStatusId = _config.DocumentActionTypeStatusId;

        foreach (WorkOrderFileEntity file in workOrder.Files.Where(f => !f.Uploaded))
        {
            if (!await fileStorage.ExistsAsync(file.StoragePath, ct))
            {
                throw new FileNotFoundException($"Required file missing from storage: {file.StoragePath}");
            }

            await using Stream stream = await fileStorage.OpenReadAsync(file.StoragePath, ct);
            await pythagorasClient.UploadWorkOrderDocumentAsync(
                workOrderId, stream, file.FileName, file.FileSize,
                parentId, actionTypeId, actionTypeStatusId, ct);

            file.Uploaded = true;
            await workOrderRepository.UpdateAsync(workOrder, ct);
        }
    }

    private async Task<int?> DiscoverWorkOrderFolderAsync(int workOrderId, CancellationToken ct)
    {
        try
        {
            IReadOnlyList<FileDocumentDirectory> folders = await pythagorasClient
                .GetWorkOrderDocumentFoldersAsync(workOrderId, ct: ct);
            return folders.Count > 0 ? folders[0].Id : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not discover document folder for work order {WorkOrderId}. Using root.", workOrderId);
            return null;
        }
    }

    private async Task MarkSubmittedAsync(WorkOrderEntity workOrder, CancellationToken ct)
    {
        workOrder.SyncStatus = WorkOrderSyncStatus.Submitted;
        workOrder.SubmittedAt = DateTimeOffset.UtcNow;
        workOrder.ErrorMessage = null;
        workOrder.NextSyncAt = DateTimeOffset.UtcNow.AddMinutes(_config.StatusCheckIntervalMinutes);
        workOrder.UpdatedAt = DateTimeOffset.UtcNow;
        await workOrderRepository.UpdateAsync(workOrder, ct);

        logger.LogInformation("WorkOrder {WorkOrderUid} submitted to Pythagoras as workOrder {PythagorasId}.",
            workOrder.Uid, workOrder.PythagorasWorkOrderId);
    }

    private async Task CleanupFilesAsync(WorkOrderEntity workOrder)
    {
        try
        {
            await fileStorage.DeleteWorkOrderFilesAsync(workOrder.Uid);
            logger.LogDebug("Cleaned up files for workOrder {WorkOrderUid}.", workOrder.Uid);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cleanup files for workOrder {WorkOrderUid}.", workOrder.Uid);
        }
    }
}
