using System.Collections.Immutable;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Shared.Data;

public interface IDataStore
{
    IEnumerable<EstateEntity> Estates { get; }
    IEnumerable<BuildingEntity> Buildings { get; }
    IEnumerable<FloorEntity> Floors { get; }
    IEnumerable<RoomEntity> Rooms { get; }
    IEnumerable<WorkOrderCategoryNode> WorkOrderCategories { get; }

    /// <summary>
    /// Maps building IDs to their pre-computed ascendant hierarchy.
    /// Key: BuildingId, Value: Triplet containing Estate/Region/Organization ascendants.
    /// </summary>
    IReadOnlyDictionary<int, BuildingAscendantTriplet> BuildingAscendants { get; }

    /// <summary>
    /// Pre-computed immutable lookup dictionary for buildings by ID.
    /// Eliminates the need to call .ToDictionary() on every query.
    /// Provides O(log n) lookups with true immutability guarantees.
    /// </summary>
    IReadOnlyDictionary<int, BuildingEntity> BuildingsById { get; }

    /// <summary>
    /// Pre-computed immutable lookup dictionary for floors by ID.
    /// Eliminates the need to call .ToDictionary() on every query.
    /// Provides O(log n) lookups with true immutability guarantees.
    /// </summary>
    IReadOnlyDictionary<int, FloorEntity> FloorsById { get; }

    /// <summary>
    /// Pre-computed immutable lookup dictionary for estates by ID.
    /// Eliminates the need to call .ToDictionary() on every query.
    /// Provides O(log n) lookups with true immutability guarantees.
    /// </summary>
    IReadOnlyDictionary<int, EstateEntity> EstatesById { get; }

    /// <summary>
    /// Pre-computed immutable lookup dictionary for rooms by ID.
    /// Eliminates the need to call .ToDictionary() on every query.
    /// Provides O(log n) lookups with true immutability guarantees.
    /// </summary>
    IReadOnlyDictionary<int, RoomEntity> RoomsById { get; }

    IReadOnlyDictionary<int, WorkOrderCategoryNode> WorkOrderCategoriesById { get; }

    /// <summary>
    /// Record action type status IDs that represent "Publiceras i Fastighetsportal".
    /// Fetched from Pythagoras during sync. Empty until first successful sync.
    /// </summary>
    ImmutableHashSet<int> PortalPublishStatusIds { get; }

    bool IsReady { get; }
    DateTimeOffset? LastRefreshUtc { get; }
    DateTimeOffset? LastAttemptUtc { get; }

    void RecordRefreshAttempt(DateTimeOffset attemptUtc);

    /// <summary>
    /// Sets the current data snapshot atomically.
    /// This is the preferred method for updating the data store.
    /// </summary>
    /// <param name="snapshot">The fully-constructed snapshot to set.</param>
    void SetSnapshot(DataSnapshot snapshot);

    /// <summary>
    /// Gets the current data snapshot.
    /// </summary>
    DataSnapshot GetCurrentSnapshot();

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}
