using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasFloorInfoMapper
{
    public static FloorInfoModel ToModel(Floor dto, IReadOnlyList<BuildingRoomModel>? rooms = null)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new FloorInfoModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name,
            PopularName = dto.PopularName,
            Height = dto.Height,
            ReferenceHeight = dto.ReferenceHeight,
            GrossFloorArea = dto.GrossFloorarea,
            GrossArea = dto.Grossarea,
            NetArea = dto.Netarea,
            BuildingId = dto.BuildingId,
            BuildingUid = dto.BuildingUid,
            BuildingName = dto.BuildingName,
            BuildingPopularName = dto.BuildingPopularName,
            BuildingOrigin = dto.BuildingOrigin,
            NumPlacedPersons = dto.NumPlacedPersons,
            Rooms = rooms
        };
    }

    public static IReadOnlyList<FloorInfoModel> ToModel(IReadOnlyList<Floor> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(floor => ToModel(floor, null)).ToArray();
    }
}
