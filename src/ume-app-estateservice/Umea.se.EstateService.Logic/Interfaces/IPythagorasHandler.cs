using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IPythagorasHandler
{
    Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<Workspace>? workspaceQuery = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default);
    Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default);
    Task<EstateModel?> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsWithPropertiesAsync(IReadOnlyCollection<int>? buildingIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EstateModel>> GetEstatesWithPropertiesAsync(IReadOnlyCollection<int>? estateIds = null, IReadOnlyCollection<int>? propertyIds = null, int? navigationId = null, CancellationToken cancellationToken = default);
}
