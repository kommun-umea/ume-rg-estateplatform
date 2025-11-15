using Umea.se.EstateService.Logic.Data.Entities;

namespace Umea.se.EstateService.Logic.Data;

public sealed class InMemoryDataStoreCacheSnapshot
{
    public DateTimeOffset LastRefreshUtc { get; init; }
    public List<EstateEntity> Estates { get; init; } = [];
    public List<BuildingEntity> Buildings { get; init; } = [];
    public List<FloorEntity> Floors { get; init; } = [];
    public List<RoomEntity> Rooms { get; init; } = [];
}

