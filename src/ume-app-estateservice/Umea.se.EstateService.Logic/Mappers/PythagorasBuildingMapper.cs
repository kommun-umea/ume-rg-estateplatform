using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasBuildingMapper
{
    public static BuildingModel ToModel(Building dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuildingModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Version = dto.Version,
            Created = dto.Created,
            Updated = dto.Updated,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GeoLocation = ToModel(dto.GeoLocation),
            PropertyTax = dto.PropertyTax,
        };
    }

    public static IReadOnlyList<BuildingModel> ToModel(IReadOnlyList<Building> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(ToModel).ToArray();
    }

    private static GeoPointModel? ToModel(GeoPoint? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new GeoPointModel(dto.X, dto.Y, dto.Rotation);
    }
}
