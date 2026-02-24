using System.Collections.Immutable;
using Umea.se.EstateService.Logic.Data;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// Helpers for seeding the in-memory IDataStore used by handlers in tests.
/// </summary>
public static class DataStoreSeeder
{
    public static void Seed(
        InMemoryDataStore dataStore,
        IEnumerable<EstateEntity>? estates = null,
        IEnumerable<BuildingEntity>? buildings = null,
        IEnumerable<FloorEntity>? floors = null,
        IEnumerable<RoomEntity>? rooms = null,
        IReadOnlyDictionary<int, BuildingAscendantTriplet>? buildingAscendants = null)
    {
        DateTimeOffset refreshTime = DateTimeOffset.UtcNow;

        DataSnapshot snapshot = new(
            estates: estates is null ? [] : [.. estates],
            buildings: buildings is null ? [] : [.. buildings],
            floors: floors is null ? [] : [.. floors],
            rooms: rooms is null ? [] : [.. rooms],
            buildingAscendants: buildingAscendants ?? ImmutableDictionary<int, BuildingAscendantTriplet>.Empty,
            refreshUtc: refreshTime
        );

        dataStore.SetSnapshot(snapshot);
        dataStore.RecordRefreshAttempt(refreshTime);
    }

    public static void Clear(InMemoryDataStore dataStore)
    {
        Seed(dataStore);
    }
}

