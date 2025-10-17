using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasFloorMapper
{
    public static FloorWithRoomsModel ToModel(Floor dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return ToModel(dto, []);
    }

    public static FloorWithRoomsModel ToModel(Floor dto, IReadOnlyList<BuildingRoomModel> rooms)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(rooms);

        return new FloorWithRoomsModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Version = dto.Version,
            Created = dto.Created,
            Updated = dto.Updated,
            Name = dto.Name,
            PopularName = dto.PopularName,
            Height = dto.Height,
            ReferenceHeight = dto.ReferenceHeight,
            GrossFloorArea = dto.GrossFloorarea,
            Rooms = rooms
        };
    }
}
