using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.ServiceAccess.Data;

namespace Umea.se.EstateService.Logic.Data;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IDataStore"/> backed by immutable snapshots.
/// </summary>
public sealed class InMemoryDataStore : IDataStore
{
    private readonly object _swapLock = new();

    private ImmutableArray<EstateEntity> _estates = [];
    private ImmutableArray<BuildingEntity> _buildings = [];
    private ImmutableArray<FloorEntity> _floors = [];
    private ImmutableArray<RoomEntity> _rooms = [];

    private int _isReady;
    private DateTimeOffset? _lastRefreshUtc;
    private DateTimeOffset? _lastAttemptUtc;
    private readonly TaskCompletionSource _initialRefreshTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IEnumerable<EstateEntity> Estates => _estates;
    public IEnumerable<BuildingEntity> Buildings => _buildings;
    public IEnumerable<FloorEntity> Floors => _floors;
    public IEnumerable<RoomEntity> Rooms => _rooms;

    public bool IsReady => Volatile.Read(ref _isReady) == 1;
    public DateTimeOffset? LastRefreshUtc
    {
        get
        {
            lock (_swapLock)
            {
                return _lastRefreshUtc;
            }
        }
    }

    public DateTimeOffset? LastAttemptUtc
    {
        get
        {
            lock (_swapLock)
            {
                return _lastAttemptUtc;
            }
        }
    }

    public IReadOnlyList<EstateEntity> EstateSnapshot => _estates;
    public IReadOnlyList<BuildingEntity> BuildingSnapshot => _buildings;
    public IReadOnlyList<FloorEntity> FloorSnapshot => _floors;
    public IReadOnlyList<RoomEntity> RoomSnapshot => _rooms;

    /// <summary>
    /// Record the timestamp for a refresh attempt regardless of outcome.
    /// </summary>
    public void RecordRefreshAttempt(DateTimeOffset attemptUtc)
    {
        lock (_swapLock)
        {
            _lastAttemptUtc = attemptUtc;
        }
    }

    /// <summary>
    /// Swap the current snapshots with new immutable collections.
    /// </summary>
    public void ReplaceSnapshots(ImmutableArray<EstateEntity> estates, ImmutableArray<BuildingEntity> buildings, ImmutableArray<FloorEntity> floors, ImmutableArray<RoomEntity> rooms, DateTimeOffset refreshUtc)
    {
        lock (_swapLock)
        {
            // Ensure readers always see a fully consistent snapshot.
            _estates = estates;
            _buildings = buildings;
            _floors = floors;
            _rooms = rooms;

            _lastRefreshUtc = refreshUtc;
            Volatile.Write(ref _isReady, 1);
        }
    }

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsReady)
        {
            return Task.CompletedTask;
        }

        return _initialRefreshTcs.Task.WaitAsync(cancellationToken);
    }

    internal void SignalInitialRefreshFailed(Exception ex)
    {
        _initialRefreshTcs.TrySetException(ex);
    }

    public bool TryLoadFromJson(string path, ILogger logger)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            logger.LogInformation("Loading in-memory data store snapshot from {Path}", path);

            string json = File.ReadAllText(path);
            InMemoryDataStoreCacheSnapshot? snapshot = JsonSerializer.Deserialize<InMemoryDataStoreCacheSnapshot>(json);

            if (snapshot is null)
            {
                logger.LogWarning("Failed to deserialize in-memory data store snapshot from {Path}", path);
                return false;
            }

            ImmutableArray<EstateEntity> estates = snapshot.Estates.ToImmutableArray();
            ImmutableArray<BuildingEntity> buildings = snapshot.Buildings.ToImmutableArray();
            ImmutableArray<FloorEntity> floors = snapshot.Floors.ToImmutableArray();
            ImmutableArray<RoomEntity> rooms = snapshot.Rooms.ToImmutableArray();

            ReplaceSnapshots(estates, buildings, floors, rooms, snapshot.LastRefreshUtc);
            RecordRefreshAttempt(snapshot.LastRefreshUtc);

            logger.LogInformation("Loaded in-memory data store snapshot with {EstateCount} estates and {BuildingCount} buildings from {Path}",
                estates.Length, buildings.Length, path);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while loading in-memory data store snapshot from {Path}", path);
            return false;
        }
    }

    public void TrySaveToJson(string path, ILogger logger)
    {
        try
        {
            DateTimeOffset? lastRefreshUtc = LastRefreshUtc;
            if (lastRefreshUtc is null)
            {
                return;
            }

            InMemoryDataStoreCacheSnapshot snapshot = new()
            {
                LastRefreshUtc = lastRefreshUtc.Value,
                Estates = [.. _estates],
                Buildings = [.. _buildings],
                Floors = [.. _floors],
                Rooms = [.. _rooms]
            };

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(snapshot);
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, overwrite: true);

            logger.LogInformation("Saved in-memory data store snapshot to {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while saving in-memory data store snapshot to {Path}", path);
        }
    }
}
