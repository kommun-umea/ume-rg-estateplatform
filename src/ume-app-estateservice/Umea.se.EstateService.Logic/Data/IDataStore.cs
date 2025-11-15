using System.Collections.Immutable;
using Umea.se.EstateService.Logic.Data.Entities;

namespace Umea.se.EstateService.ServiceAccess.Data;

public interface IDataStore
{
    IEnumerable<EstateEntity> Estates { get; }
    IEnumerable<BuildingEntity> Buildings { get; }
    IEnumerable<FloorEntity> Floors { get; }
    IEnumerable<RoomEntity> Rooms { get; }

    bool IsReady { get; }
    DateTimeOffset? LastRefreshUtc { get; }
    DateTimeOffset? LastAttemptUtc { get; }

    void RecordRefreshAttempt(DateTimeOffset attemptUtc);
    void ReplaceSnapshots(ImmutableArray<EstateEntity> estates, ImmutableArray<BuildingEntity> buildings, ImmutableArray<FloorEntity> floors, ImmutableArray<RoomEntity> rooms, DateTimeOffset refreshUtc);

    Task WaitUntilReadyAsync(CancellationToken cancellationToken = default);
}
