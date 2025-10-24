using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Interfaces;

public interface IPythagorasHandler
{
    Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(PythagorasQuery<BuildingInfo>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingInfoAsync(PythagorasQuery<BuildingInfo>? query = null, int? navigationFolderId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingRoomModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsWithRoomsAsync(int buildingId, PythagorasQuery<Floor>? floorQuery = null, PythagorasQuery<BuildingWorkspace>? workspaceQuery = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingAscendantModel>> GetBuildingAscendantsAsync(int buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetRoomsAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(PythagorasQuery<Floor>? query = null, CancellationToken cancellationToken = default);
    Task<BuildingInfoModel?> GetBuildingByIdAsync(int buildingId, BuildingIncludeOptions includeOptions = BuildingIncludeOptions.None, CancellationToken cancellationToken = default);
}
