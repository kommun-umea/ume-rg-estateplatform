using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using DomainMarkerType = Umea.se.EstateService.Shared.Models.MarkerType;
using TransportMarkerType = Umea.se.EstateService.ServiceAccess.Pythagoras.Dto.MarkerType;

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

    private static DomainMarkerType ToModel(TransportMarkerType markerType)
    {
        int numeric = (int)markerType;
        if (Enum.IsDefined(typeof(DomainMarkerType), numeric))
        {
            return (DomainMarkerType)numeric;
        }

        return DomainMarkerType.Unknown;
    }

    private static GeoPointModel ToModel(GeoPoint? dto)
    {
        if (dto is null)
        {
            return new GeoPointModel();
        }

        return new GeoPointModel
        {
            X = dto.X,
            Y = dto.Y,
            Rotation = dto.Rotation
        };
    }
}
