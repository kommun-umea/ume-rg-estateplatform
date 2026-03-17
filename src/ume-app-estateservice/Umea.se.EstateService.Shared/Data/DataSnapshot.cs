using System.Collections.Immutable;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Shared.Data;

/// <summary>
/// Immutable snapshot of estate service data.
/// Designed for lock-free, consistent reads in a read-heavy workload.
/// All collections are immutable, and the snapshot is replaced atomically via a single volatile reference.
/// Uses ImmutableDictionary for true immutability guarantees.
/// </summary>
public sealed class DataSnapshot
{
    public static readonly DataSnapshot Empty = new();

    public ImmutableArray<EstateEntity> Estates { get; }
    public ImmutableArray<BuildingEntity> Buildings { get; }
    public ImmutableArray<FloorEntity> Floors { get; }
    public ImmutableArray<RoomEntity> Rooms { get; }
    public ImmutableArray<WorkOrderCategoryNode> WorkOrderCategories { get; }
    public IReadOnlyDictionary<int, BuildingAscendantTriplet> BuildingAscendants { get; }
    public ImmutableDictionary<int, EstateEntity> EstatesById { get; }
    public ImmutableDictionary<int, BuildingEntity> BuildingsById { get; }
    public ImmutableDictionary<int, FloorEntity> FloorsById { get; }
    public ImmutableDictionary<int, RoomEntity> RoomsById { get; }
    public ImmutableDictionary<int, WorkOrderCategoryNode> WorkOrderCategoriesById { get; }
    public DateTimeOffset? LastRefreshUtc { get; }
    public bool IsReady { get; }

    /// <summary>
    /// Private constructor for the empty sentinel snapshot.
    /// </summary>
    private DataSnapshot()
    {
        Estates = [];
        Buildings = [];
        Floors = [];
        Rooms = [];
        WorkOrderCategories = [];
        BuildingAscendants = ImmutableDictionary<int, BuildingAscendantTriplet>.Empty;
        EstatesById = [];
        BuildingsById = [];
        FloorsById = [];
        RoomsById = [];
        WorkOrderCategoriesById = ImmutableDictionary<int, WorkOrderCategoryNode>.Empty;
        IsReady = false;
        LastRefreshUtc = null;
    }

    /// <summary>
    /// Constructor for creating a new snapshot with data.
    /// Pre-computes all lookup dictionaries during construction using ImmutableDictionary
    /// for true immutability guarantees and efficient lookups.
    /// </summary>
    public DataSnapshot(
        ImmutableArray<EstateEntity> estates,
        ImmutableArray<BuildingEntity> buildings,
        ImmutableArray<FloorEntity> floors,
        ImmutableArray<RoomEntity> rooms,
        IReadOnlyDictionary<int, BuildingAscendantTriplet> buildingAscendants,
        DateTimeOffset refreshUtc,
        ImmutableArray<WorkOrderCategoryNode> workOrderCategories = default)
    {
        Estates = estates;
        Buildings = buildings;
        Floors = floors;
        Rooms = rooms;
        WorkOrderCategories = workOrderCategories.IsDefault ? [] : workOrderCategories;
        BuildingAscendants = buildingAscendants;
        LastRefreshUtc = refreshUtc;
        IsReady = true;

        // Pre-compute lookup dictionaries once during construction
        // ImmutableDictionary provides true immutability and efficient O(log n) lookups
        EstatesById = estates.ToImmutableDictionary(e => e.Id);
        BuildingsById = buildings.ToImmutableDictionary(b => b.Id);
        FloorsById = floors.ToImmutableDictionary(f => f.Id);
        RoomsById = rooms.ToImmutableDictionary(r => r.Id);
        WorkOrderCategoriesById = WorkOrderCategories.ToImmutableDictionary(c => c.Id);

        // Wire up navigation properties so handlers can traverse the hierarchy.
        // Skip if already populated (e.g. by PythagorasDataRefreshService before snapshot creation).
        WireUpNavigationProperties();
    }

    private void WireUpNavigationProperties()
    {
        if (Buildings.IsEmpty)
        {
            return;
        }

        // Skip if already populated (e.g. by PythagorasDataRefreshService before snapshot creation)
        if (Buildings[0].Floors.Count > 0 || Buildings[0].Rooms.Count > 0)
        {
            return;
        }

        foreach (FloorEntity floor in Floors)
        {
            if (BuildingsById.TryGetValue(floor.BuildingId, out BuildingEntity? building))
            {
                building.Floors.Add(floor);
            }
        }

        foreach (RoomEntity room in Rooms)
        {
            if (BuildingsById.TryGetValue(room.BuildingId, out BuildingEntity? building))
            {
                building.Rooms.Add(room);
            }
        }
    }
}
