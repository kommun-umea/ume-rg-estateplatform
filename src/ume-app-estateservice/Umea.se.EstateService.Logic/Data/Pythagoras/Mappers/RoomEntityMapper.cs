using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.Logic.Data.Mappers;

/// <summary>
/// Maps Workspace DTOs from Pythagoras API to RoomEntity objects.
/// </summary>
public static class RoomEntityMapper
{
    /// <summary>
    /// Converts a Workspace DTO to a RoomEntity.
    /// </summary>
    /// <param name="dto">The Workspace DTO from Pythagoras API.</param>
    /// <returns>A mapped RoomEntity.</returns>
    public static RoomEntity ToEntity(Workspace dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new RoomEntity
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.GrossArea,
            NetArea = dto.NetArea,
            Capacity = dto.Capacity,
            BuildingId = dto.BuildingId ?? 0,
            FloorId = dto.FloorId,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(dto.Updated / 1000)
        };
    }

    /// <summary>
    /// Converts a collection of Workspace DTOs to RoomEntity objects.
    /// </summary>
    /// <param name="dtos">The collection of Workspace DTOs.</param>
    /// <returns>A list of mapped RoomEntity objects.</returns>
    public static List<RoomEntity> ToEntities(IReadOnlyList<Workspace> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        List<RoomEntity> entities = new(dtos.Count);
        foreach (Workspace dto in dtos)
        {
            entities.Add(ToEntity(dto));
        }

        return entities;
    }
}
