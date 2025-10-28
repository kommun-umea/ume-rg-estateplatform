using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasWorkspaceMapper
{
    public static BuildingRoomModel ToModel(BuildingWorkspace dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuildingRoomModel
        {
            Id = dto.Id,
            Created = dto.Created,
            Updated = dto.Updated,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName,
            GrossArea = dto.GrossArea,
            NetArea = dto.NetArea,
            UpliftedArea = dto.UpliftedArea,
            CommonArea = dto.CommonArea,
            Capacity = dto.Capacity,
            OptimalCapacity = dto.OptimalCapacity,
            FloorId = dto.FloorId,
            FloorName = dto.FloorName,
            FloorPopularName = dto.FloorPopularName,
            BuildingId = dto.BuildingId,
            BuildingName = dto.BuildingName ?? string.Empty,
            BuildingPopularName = dto.BuildingPopularName,
        };
    }

    public static IReadOnlyList<BuildingRoomModel> ToModel(IReadOnlyList<BuildingWorkspace> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        BuildingRoomModel[] buffer = new BuildingRoomModel[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            buffer[i] = ToModel(dtos[i]);
        }

        return buffer;
    }

    public static RoomModel ToDomain(Workspace dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RoomModel
        {
            Id = dto.Id,
            Version = dto.Version,
            Created = dto.Created,
            Updated = dto.Updated,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName,
            GrossArea = dto.GrossArea,
            NetArea = dto.NetArea,
            CommonArea = dto.CommonArea,
            FloorId = dto.FloorId,
            FloorName = dto.FloorName,
            FloorPopularName = dto.PopularName,
            BuildingId = dto.BuildingId,
            BuildingName = dto.BuildingName,
            BuildingPopularName = dto.BuildingPopularName,
        };
    }

    public static IReadOnlyList<RoomModel> ToModel(IReadOnlyList<Workspace> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        RoomModel[] buffer = new RoomModel[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            buffer[i] = ToDomain(dtos[i]);
        }

        return buffer;
    }
}
