using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Handlers.Images;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Sync;

public sealed class ImagePreWarmHandler(
    IServiceScopeFactory scopeFactory,
    IDataStore dataStore,
    ILogger<ImagePreWarmHandler> logger)
{
    private const int ThumbnailWidth = 300;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    /// <summary>
    /// Attempt to start an image pre-warm in the background.
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
    /// Run image pre-warm if not already running. Used by the cron loop.
    /// </summary>
    public async Task SyncAllBuildingsAsync(CancellationToken ct)
    {
        if (!await _syncGate.WaitAsync(0, ct))
        {
            logger.LogInformation("Image pre-warm already running — skipping.");
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
        int warmed = 0, skipped = 0, failed = 0;

        logger.LogInformation("Image pre-warm starting for {Count} buildings.",
            dataStore.Buildings.Count());

        // IBuildingImageService is scoped — resolve per sync run
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IBuildingImageService imageService = scope.ServiceProvider.GetRequiredService<IBuildingImageService>();

        foreach (BuildingEntity building in dataStore.Buildings)
        {
            ct.ThrowIfCancellationRequested();

            if (building.ImageIds is not { Count: > 0 })
            {
                skipped++;
                continue;
            }

            int primaryImageId = building.ImageIds[0];

            try
            {
                // FusionCache GetOrSetAsync: L1/L2 hit = instant, miss = Pythagoras fetch + normalize + cache
                await imageService.GetImageResultAsync(
                    building.Id,
                    primaryImageId,
                    maxWidth: null,
                    maxHeight: null,
                    ct);

                // Pre-warm the most common thumbnail size (list views)
                await imageService.GetImageResultAsync(
                    building.Id,
                    primaryImageId,
                    maxWidth: ThumbnailWidth,
                    maxHeight: null,
                    ct);

                warmed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Image pre-warm failed for building {BuildingId}, image {ImageId}",
                    building.Id, primaryImageId);
                failed++;
            }
        }

        logger.LogInformation("Image pre-warm complete. Warmed={Warmed}, Skipped={Skipped}, Failed={Failed}",
            warmed, skipped, failed);
    }
}
