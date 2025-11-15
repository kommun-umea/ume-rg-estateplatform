using System.Collections.Immutable;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Logic.Data.Entities;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Helpers for seeding the in-memory IDataStore used by v2 handlers in tests.
/// </summary>
public static class DataStoreSeeder
{
    public static void Seed(
        InMemoryDataStore dataStore,
        IEnumerable<EstateEntity>? estates = null,
        IEnumerable<BuildingEntity>? buildings = null,
        IEnumerable<FloorEntity>? floors = null,
        IEnumerable<RoomEntity>? rooms = null)
    {
        ImmutableArray<EstateEntity> estateArray = estates is null ? [] : estates.ToImmutableArray();
        ImmutableArray<BuildingEntity> buildingArray = buildings is null ? [] : buildings.ToImmutableArray();
        ImmutableArray<FloorEntity> floorArray = floors is null ? [] : floors.ToImmutableArray();
        ImmutableArray<RoomEntity> roomArray = rooms is null ? [] : rooms.ToImmutableArray();

        dataStore.ReplaceSnapshots(estateArray, buildingArray, floorArray, roomArray, DateTimeOffset.UtcNow);
        dataStore.RecordRefreshAttempt(DateTimeOffset.UtcNow);
    }

    public static void Clear(InMemoryDataStore dataStore)
    {
        Seed(dataStore);
    }
}

