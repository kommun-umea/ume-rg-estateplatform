using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

internal static class PythagorasWorkspaceMapper
{
    public static BuildingWorkspaceModel ToDomain(BuildingWorkspace dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuildingWorkspaceModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Created = dto.Created,
            Updated = dto.Updated,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName,
            GrossArea = dto.GrossArea,
            NetArea = dto.NetArea,
            UpliftedArea = dto.UpliftedArea,
            CommonArea = dto.CommonArea,
            Cost = dto.Cost,
            Price = dto.Price,
            Capacity = dto.Capacity,
            OptimalCapacity = dto.OptimalCapacity,
            FloorId = dto.FloorId,
            FloorUid = dto.FloorUid,
            FloorName = dto.FloorName,
            FloorPopularName = dto.FloorPopularName,
            BuildingId = dto.BuildingId,
            BuildingUid = dto.BuildingUid,
            BuildingName = dto.BuildingName ?? string.Empty,
            BuildingPopularName = dto.BuildingPopularName,
            BuildingOrigin = dto.BuildingOrigin,
            StatusName = dto.StatusName,
            StatusColor = dto.StatusColor,
            RentalStatusName = dto.RentalStatusName,
            RentalStatusColor = dto.RentalStatusColor
        };
    }

    public static IReadOnlyList<BuildingWorkspaceModel> ToDomain(IReadOnlyList<BuildingWorkspace> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        BuildingWorkspaceModel[] buffer = new BuildingWorkspaceModel[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            buffer[i] = ToDomain(dtos[i]);
        }

        return buffer;
    }

    public static WorkspaceModel ToDomain(Workspace dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new WorkspaceModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Version = dto.Version,
            Created = dto.Created,
            Updated = dto.Updated,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName,
            GrossArea = dto.GrossArea,
            NetArea = dto.NetArea,
            UpliftedArea = dto.UpliftedArea,
            CommonArea = dto.CommonArea,
            Cost = dto.Cost,
            Price = dto.Price,
            Capacity = dto.Capacity,
            OptimalCapacity = dto.OptimalCapacity
        };
    }

    public static IReadOnlyList<WorkspaceModel> ToDomain(IReadOnlyList<Workspace> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        WorkspaceModel[] buffer = new WorkspaceModel[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            buffer[i] = ToDomain(dtos[i]);
        }

        return buffer;
    }
}
