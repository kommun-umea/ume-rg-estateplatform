using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Handlers.Images;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.Toolkit.Images;

namespace Umea.se.EstateService.Logic.Sync;

public sealed class ImagePreWarmHandler(
    IServiceScopeFactory scopeFactory,
    IDataStore dataStore,
    ILogger<ImagePreWarmHandler> logger)
{
    private const int ThumbnailWidth = 300;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    private static readonly IReadOnlyList<ImageVariantRequest> PreWarmVariants =
    [
        new ImageVariantRequest(MaxWidth: null, MaxHeight: null),
        new ImageVariantRequest(MaxWidth: ThumbnailWidth, MaxHeight: null),
    ];

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
        (int BuildingId, int ImageId, Exception Error)? firstFailure = null;

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
                await imageService.PreWarmImageAsync(
                    building.Id,
                    primaryImageId,
                    PreWarmVariants,
                    ct);

                warmed++;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Image pre-warm failed for building {BuildingId}, image {ImageId}",
                    building.Id, primaryImageId);
                failed++;
                firstFailure ??= (building.Id, primaryImageId, ex);
            }

            // Yield briefly between items as a backpressure point for
            // ImageSharp allocations, background L2 writes, and normal app
            // work. ~37 s total over 1461 buildings — negligible for a
            // nightly batch.
            await Task.Delay(TimeSpan.FromMilliseconds(25), ct);
        }

        if (firstFailure is null)
        {
            logger.LogInformation("Image pre-warm complete. Warmed={Warmed}, Skipped={Skipped}, Failed={Failed}",
                warmed, skipped, failed);
            return;
        }

        (int firstFailedBuildingId, int firstFailedImageId, Exception firstError) = firstFailure.Value;
        logger.LogWarning(
            "Image pre-warm complete with failures. Warmed={Warmed}, Skipped={Skipped}, Failed={Failed}. " +
            "First failure: building {FirstFailedBuildingId} image {FirstFailedImageId} — {FirstFailureType}: {FirstFailureMessage}",
            warmed, skipped, failed,
            firstFailedBuildingId, firstFailedImageId, firstError.GetType().Name, firstError.Message);
    }
}
