using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.Logic.Data.Pythagoras.Mappers;

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
            Name = dto.Name ?? string.Empty,
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
    public static List<RoomEntity> ToEntities(IReadOnlyList<Workspace> dtos)
        => MapperUtilities.ToEntities(dtos, ToEntity);
}
