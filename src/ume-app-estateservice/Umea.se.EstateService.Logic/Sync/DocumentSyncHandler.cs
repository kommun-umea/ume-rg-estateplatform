using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.DataStore;
using Umea.se.EstateService.DataStore.Entities;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Sync;

public sealed class DocumentSyncHandler(
    IPythagorasClient pythagorasClient,
    IDataStore dataStore,
    IDbContextFactory<EstateDbContext> dbContextFactory,
    ILogger<DocumentSyncHandler> logger)
{
    private const int MaxParallelInfoRequests = 10;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    /// <summary>
    /// Attempt to start a document sync in the background.
    /// Returns true if started, false if already running.
    /// </summary>
    public bool TryTriggerSync()
    {
        if (!_syncGate.Wait(0))
            return false;

        _ = Task.Run(async () =>
        {
            try { await RunSyncAsync(CancellationToken.None); }
            finally { _syncGate.Release(); }
        });

        return true;
    }

    /// <summary>
    /// Run a document sync if not already running. Used by the cron loop.
    /// </summary>
    public async Task SyncAllBuildingsAsync(CancellationToken ct)
    {
        if (!await _syncGate.WaitAsync(0, ct))
        {
            logger.LogInformation("Document sync already running — skipping.");
            return;
        }

        try
        {
            await RunSyncAsync(ct);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        // Global pre-check: has anything changed since last sync?
        UiListDataResponse<FileDocument> global =
            await pythagorasClient.GetDocumentListAsync(maxResults: 1, orderBy: "updated", orderAsc: false, ct);

        int totalCount = global.TotalSize;
        long latestUpdated = global.Data is { Count: > 0 } ? global.Data[0].Updated : 0;

        if (await IsUnchangedAsync(totalCount, latestUpdated, ct))
        {
            logger.LogInformation("Document sync skipped — no changes detected (count={Count}, latest={Latest}).",
                totalCount, latestUpdated);
            return;
        }

        logger.LogInformation("Document sync starting for {Count} buildings.",
            dataStore.Buildings.Count());

        int synced = 0;
        foreach (BuildingEntity building in dataStore.Buildings)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await SyncBuildingAsync(building.Id, ct);
                synced++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Document sync failed for building {BuildingId}",
                    building.Id);
            }
        }

        logger.LogInformation("Document sync complete. {Synced}/{Total} buildings.",
            synced, dataStore.Buildings.Count());

        await SaveDocumentFingerprintAsync(totalCount, latestUpdated, ct);
    }

    private async Task<bool> IsUnchangedAsync(int totalCount, long latestUpdated, CancellationToken ct)
    {
        await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(ct);

        DataSyncMetadata? metadata = await context.Set<DataSyncMetadata>()
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync(ct);

        if (metadata is null
            || metadata.DocumentTotalCount is null
            || metadata.DocumentLatestUpdatedEpoch is null)
        {
            return false;
        }

        return metadata.DocumentTotalCount == totalCount
            && metadata.DocumentLatestUpdatedEpoch == latestUpdated;
    }

    private async Task SaveDocumentFingerprintAsync(int totalCount, long latestUpdated, CancellationToken ct)
    {
        await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(ct);

        DataSyncMetadata? metadata = await context.Set<DataSyncMetadata>()
            .OrderBy(m => m.Id)
            .FirstOrDefaultAsync(ct);

        if (metadata is not null)
        {
            metadata.DocumentTotalCount = totalCount;
            metadata.DocumentLatestUpdatedEpoch = latestUpdated;
            await context.SaveChangesAsync(ct);
        }
    }

    private async Task SyncBuildingAsync(int buildingId, CancellationToken ct)
    {
        UiListDataResponse<FileDocument> response =
            await pythagorasClient.GetBuildingDocumentListAsync(buildingId, maxResults: null, ct);

        if (response.Data is not { Count: > 0 })
        {
            await DeleteBuildingDocumentsAsync(buildingId, ct);
            return;
        }

        Dictionary<int, FileDocumentInfo> infoLookup =
            await GetDocumentInfoLookupAsync(response.Data, ct);

        List<BuildingDocumentEntity> rows = [];
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (FileDocument doc in response.Data)
        {
            if (infoLookup.TryGetValue(doc.Id, out FileDocumentInfo? info)
                && info.RecordStatusId.HasValue
                && dataStore.PortalPublishStatusIds.Contains(info.RecordStatusId.Value)
                && info.VersionRank == 1)
            {
                rows.Add(new BuildingDocumentEntity
                {
                    BuildingId = buildingId,
                    DocumentId = doc.Id,
                    Name = doc.Name,
                    SizeInBytes = doc.DataSize,
                    CategoryId = info.RecordActionTypeId,
                    CategoryName = info.RecordActionTypeName,
                    FetchedAtUtc = now,
                });
            }
        }

        EstateDbContext strategyContext = await dbContextFactory.CreateDbContextAsync(ct);
        await using (strategyContext)
        {
            IExecutionStrategy strategy = strategyContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using EstateDbContext context = await dbContextFactory.CreateDbContextAsync(ct);
                await using IDbContextTransaction tx =
                    await context.Database.BeginTransactionAsync(ct);

                await context.BuildingDocuments
                    .Where(r => r.BuildingId == buildingId)
                    .ExecuteDeleteAsync(ct);

                if (rows.Count > 0)
                {
                    context.BuildingDocuments.AddRange(rows);
                    await context.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);
            });
        }
    }

    private async Task DeleteBuildingDocumentsAsync(int buildingId, CancellationToken ct)
    {
        await using EstateDbContext context =
            await dbContextFactory.CreateDbContextAsync(ct);

        await context.BuildingDocuments
            .Where(r => r.BuildingId == buildingId)
            .ExecuteDeleteAsync(ct);
    }

    private async Task<Dictionary<int, FileDocumentInfo>> GetDocumentInfoLookupAsync(
        IReadOnlyList<FileDocument> documents, CancellationToken ct)
    {
        using SemaphoreSlim semaphore = new(MaxParallelInfoRequests);
        Task<FileDocumentInfo?>[] tasks = documents.Select(async doc =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                return await pythagorasClient.GetDocumentInfoAsync(doc.Id, ct);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        FileDocumentInfo?[] results = await Task.WhenAll(tasks);

        Dictionary<int, FileDocumentInfo> lookup = [];
        foreach (FileDocumentInfo? info in results)
        {
            if (info is not null)
            {
                lookup[info.Id] = info;
            }
        }
        return lookup;
    }
}
