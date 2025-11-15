using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IPythagorasHandler
{
    Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(bool includeBuildings = true, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(int[]? buildingIds = null, int? estateId = null, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetRoomsAsync(int[]? roomIds = null, int? buildingId = null, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(int[]? floorIds = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default);
    Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EstateModel>> GetEstatesWithPropertiesAsync(IReadOnlyCollection<int>? estateIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default);
}

public interface IPythagorasHandlerV2
{
    Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(bool includeBuildings = true, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(int[]? buildingIds = null, int? estateId = null, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetRoomsAsync(int[]? roomIds = null, int? buildingId = null, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(int[]? floorIds = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default);
    Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EstateModel>> GetEstatesWithPropertiesAsync(IReadOnlyCollection<int>? estateIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingLocationModel>> GetBuildingGeolocationsAsync(CancellationToken cancellationToken = default);
}
