using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.Logic.Data.Mappers;

/// <summary>
/// Maps Floor DTOs from Pythagoras API to FloorEntity objects.
/// </summary>
public static class FloorEntityMapper
{
    /// <summary>
    /// Converts a Floor DTO to a FloorEntity.
    /// </summary>
    /// <param name="dto">The Floor DTO from Pythagoras API.</param>
    /// <returns>A mapped FloorEntity.</returns>
    public static FloorEntity ToEntity(Floor dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new FloorEntity
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.Grossarea,
            NetArea = dto.Netarea,
            Height = dto.Height,
            BuildingId = dto.BuildingId ?? 0,
            UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(dto.Updated / 1000),
            Rooms = [] // Will be populated during relationship linking
        };
    }

    /// <summary>
    /// Converts a collection of Floor DTOs to FloorEntity objects.
    /// </summary>
    /// <param name="dtos">The collection of Floor DTOs.</param>
    /// <returns>A list of mapped FloorEntity objects.</returns>
    public static List<FloorEntity> ToEntities(IReadOnlyList<Floor> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        List<FloorEntity> entities = new(dtos.Count);
        foreach (Floor dto in dtos)
        {
            entities.Add(ToEntity(dto));
        }

        return entities;
    }
}
