using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Data;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IDataStore"/> backed by immutable snapshots.
/// Uses a single volatile reference to an immutable DataSnapshot for lock-free, consistent reads.
/// </summary>
public sealed class InMemoryDataStore : IDataStore
{
    private readonly object _swapLock = new();

    private volatile DataSnapshot _snapshot = DataSnapshot.Empty;
    private DateTimeOffset? _lastAttemptUtc;
    private TaskCompletionSource _initialRefreshTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Delegate all data access to the current snapshot (lock-free reads)
    public IEnumerable<EstateEntity> Estates => _snapshot.Estates;
    public IEnumerable<BuildingEntity> Buildings => _snapshot.Buildings;
    public IEnumerable<FloorEntity> Floors => _snapshot.Floors;
    public IEnumerable<RoomEntity> Rooms => _snapshot.Rooms;
    public IEnumerable<WorkOrderCategoryNode> WorkOrderCategories => _snapshot.WorkOrderCategories;
    public IReadOnlyDictionary<int, BuildingAscendantTriplet> BuildingAscendants => _snapshot.BuildingAscendants;
    public IReadOnlyDictionary<int, BuildingEntity> BuildingsById => _snapshot.BuildingsById;
    public IReadOnlyDictionary<int, FloorEntity> FloorsById => _snapshot.FloorsById;
    public IReadOnlyDictionary<int, EstateEntity> EstatesById => _snapshot.EstatesById;
    public IReadOnlyDictionary<int, RoomEntity> RoomsById => _snapshot.RoomsById;
    public IReadOnlyDictionary<int, WorkOrderCategoryNode> WorkOrderCategoriesById => _snapshot.WorkOrderCategoriesById;

    public bool IsReady => _snapshot.IsReady;
    public DateTimeOffset? LastRefreshUtc => _snapshot.LastRefreshUtc;

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
    /// Sets the current data snapshot atomically.
    /// This is the preferred method for updating the data store.
    /// </summary>
    public void SetSnapshot(DataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_swapLock)
        {
            // Single atomic reference swap - readers see either old or new snapshot, never partial state
            _snapshot = snapshot;

            // If a previous failure faulted the TCS, create a fresh one and immediately signal it
            // so both existing waiters (on the old faulted TCS) and new waiters see completion.
            if (_initialRefreshTcs.Task.IsFaulted)
            {
                _initialRefreshTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            _initialRefreshTcs.TrySetResult();
        }
    }

    /// <summary>
    /// Gets the current data snapshot.
    /// </summary>
    public DataSnapshot GetCurrentSnapshot() => _snapshot;

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsReady)
        {
            return Task.CompletedTask;
        }

        // Read TCS under lock — SetSnapshot may replace it after recovering from a faulted state
        Task tcsTask;
        lock (_swapLock)
        {
            tcsTask = _initialRefreshTcs.Task;
        }

        return tcsTask.WaitAsync(cancellationToken);
    }

    internal void SignalInitialRefreshFailed(Exception ex)
    {
        lock (_swapLock)
        {
            _initialRefreshTcs.TrySetException(ex);
        }
    }

}
