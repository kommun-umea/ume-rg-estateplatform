using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public interface IEstateDataQueryHandler
{
    void StampBuildingImageUrls(IEnumerable<PythagorasDocument> documents);
    Task<IReadOnlyList<EstateModel>> GetEstatesWithBuildingsAsync(bool includeBuildings = true, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingInfoModel>> GetBuildingsAsync(int[]? buildingIds = null, int? estateId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetBuildingWorkspacesAsync(int buildingId, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetBuildingFloorsAsync(int buildingId, bool includeRooms = false, QueryArgs? floorsQueryArgs = null, QueryArgs? roomsQueryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomModel>> GetRoomsAsync(int[]? roomIds = null, int? buildingId = null, int? floorId = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> GetBuildingWorkspaceStatsAsync(QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloorInfoModel>> GetFloorsAsync(int[]? floorIds = null, QueryArgs? queryArgs = null, CancellationToken cancellationToken = default);
    Task<BuildingInfoModel> GetBuildingByIdAsync(int buildingId, CancellationToken cancellationToken = default);
    Task<EstateModel> GetEstateByIdAsync(int estateId, bool includeBuildings = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BusinessTypeModel>> GetBusinessTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingLocationModel>> GetBuildingGeolocationsAsync(CancellationToken cancellationToken = default);
}
