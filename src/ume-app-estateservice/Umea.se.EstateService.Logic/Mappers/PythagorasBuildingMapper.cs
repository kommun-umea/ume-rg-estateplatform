using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;
using TransportMarkerType = Umea.se.EstateService.ServiceAccess.Pythagoras.Enum.PythMarkerType;

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
            MarkerType = ToModel(dto.MarkerType),
            GeoLocation = ToModel(dto.GeoLocation),
            Origin = dto.Origin ?? string.Empty,
            PropertyTax = dto.PropertyTax,
            UseWeightsInWorkspaceAreaDistribution = dto.UseWeightsInWorkspaceAreaDistribution
        };
    }

    public static IReadOnlyList<BuildingModel> ToModel(IReadOnlyList<Building> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(ToModel).ToArray();
    }

    private static MarkerTypeEnum ToModel(TransportMarkerType markerType)
    {
        int numeric = (int)markerType;
        if (System.Enum.IsDefined(typeof(MarkerTypeEnum), numeric))
        {
            return (MarkerTypeEnum)numeric;
        }

        return MarkerTypeEnum.Unknown;
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
