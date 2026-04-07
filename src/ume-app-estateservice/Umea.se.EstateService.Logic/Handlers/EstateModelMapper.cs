using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

internal static class EstateModelMapper
{
    public static FloorInfoModel MapFloor(FloorEntity floor, BuildingEntity? building, IReadOnlyList<RoomModel>? rooms = null)
    {
        return new FloorInfoModel
        {
            Id = floor.Id,
            Uid = floor.Uid,
            Name = floor.Name,
            PopularName = floor.PopularName,
            Height = floor.Height,
            GrossArea = floor.GrossArea,
            NetArea = floor.NetArea,
            BuildingId = floor.BuildingId,
            BuildingName = building?.Name,
            BuildingPopularName = building?.PopularName,
            Rooms = rooms
        };
    }

    public static RoomModel MapRoom(RoomEntity room, BuildingEntity? building, FloorEntity? floor)
    {
        return new RoomModel
        {
            Id = room.Id,
            Name = room.Name,
            PopularName = room.PopularName,
            GrossArea = room.GrossArea,
            NetArea = room.NetArea,
            Capacity = room.Capacity,
            BuildingId = room.BuildingId,
            BuildingName = building?.Name,
            BuildingPopularName = building?.PopularName,
            FloorId = room.FloorId,
            FloorName = floor?.Name,
            FloorPopularName = floor?.PopularName
        };
    }

    public static RoomModel MapRoom(
        RoomEntity room,
        IReadOnlyDictionary<int, BuildingEntity> buildingsById,
        IReadOnlyDictionary<int, FloorEntity> floorsById)
    {
        buildingsById.TryGetValue(room.BuildingId, out BuildingEntity? building);

        FloorEntity? floor = null;
        if (room.FloorId.HasValue)
        {
            floorsById.TryGetValue(room.FloorId.Value, out floor);
        }

        return MapRoom(room, building, floor);
    }

    public static EstateModel MapEstate(EstateEntity estate, bool includeBuildings = true)
    {
        return new EstateModel
        {
            Id = estate.Id,
            Uid = estate.Uid,
            Name = estate.Name,
            PopularName = estate.PopularName,
            GrossArea = estate.GrossArea,
            NetArea = estate.NetArea,
            Address = estate.Address,
            GeoLocation = estate.GeoLocation,
            BuildingCount = estate.BuildingCount,
            Buildings = includeBuildings ? [.. estate.Buildings.Select(MapBuilding)] : [],
            ExtendedProperties = new EstateExtendedPropertiesModel
            {
                PropertyDesignation = estate.PropertyDesignation,
                OperationalArea = estate.OperationalArea,
                AdministrativeArea = estate.AdministrativeArea,
                MunicipalityArea = estate.MunicipalityArea,
                ExternalOwnerInfo = MapExternalOwnerInfo(estate.ExternalOwnerStatus, estate.ExternalOwnerName, estate.ExternalOwnerNote)
            }
        };
    }

    public static BuildingModel MapBuilding(BuildingEntity building)
    {
        return new BuildingModel
        {
            Id = building.Id,
            Uid = building.Uid,
            Name = building.Name,
            PopularName = building.PopularName,
            Address = building.Address,
            GeoLocation = building.GeoLocation,
        };
    }

    /// <summary>
    /// Maps a building entity to a BuildingInfoModel.
    /// ImageIds, NumDocuments, and BackgroundCacheFetchedAtUtc are kept fresh on the entity
    /// by write-through from BuildingBackgroundCache, so no cache overlay is needed.
    /// </summary>
    public static BuildingInfoModel MapBuildingInfo(
        BuildingEntity building,
        BuildingAscendantModel? estateAscendant = null,
        BuildingAscendantModel? regionAscendant = null,
        BuildingAscendantModel? organizationAscendant = null)
    {
        BuildingNoticeBoardModel? noticeBoardModel = null;
        if (building.NoticeBoard != null)
        {
            bool isActive = (building.NoticeBoard.StartDate is null || building.NoticeBoard.StartDate.Value.Date <= DateTime.Today)
                         && (building.NoticeBoard.EndDate is null || building.NoticeBoard.EndDate.Value.Date >= DateTime.Today);

            if (isActive)
            {
                noticeBoardModel = new BuildingNoticeBoardModel
                {
                    Text = building.NoticeBoard.Text,
                    StartDate = building.NoticeBoard.StartDate,
                    EndDate = building.NoticeBoard.EndDate
                };
            }
        }

        BuildingExtendedPropertiesModel extendedProperties = new()
        {
            BlueprintAvailable = building.BlueprintAvailable,
            YearOfConstruction = building.YearOfConstruction,
            ExternalOwnerInfo = MapExternalOwnerInfo(building.ExternalOwnerStatus, building.ExternalOwnerName, building.ExternalOwnerNote),
            PropertyDesignation = building.PropertyDesignation,
            NoticeBoard = noticeBoardModel,
            ContactPersons = building.ContactPersons
        };

        return new BuildingInfoModel
        {
            Id = building.Id,
            Uid = building.Uid,
            Name = building.Name,
            PopularName = building.PopularName,
            Address = building.Address,
            GeoLocation = building.GeoLocation,
            GrossArea = building.GrossArea,
            NetArea = building.NetArea,
            Estate = estateAscendant,
            Region = regionAscendant,
            Organization = organizationAscendant,
            ExtendedProperties = extendedProperties,
            WorkOrderTypes = building.WorkOrderTypes,
            NumFloors = building.NumFloors,
            NumRooms = building.NumRooms,
            NumDocuments = building.NumDocuments,
            ImageUrl = EstateDataQueryHandler.GetBuildingImageUrl(building)
        };
    }

    private static ExternalOwnerInfoModel? MapExternalOwnerInfo(string? status, string? name, string? note)
    {
        if (string.IsNullOrEmpty(status) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(note))
        {
            return null;
        }

        return new ExternalOwnerInfoModel
        {
            Status = status,
            Name = name,
            Note = note
        };
    }
}
