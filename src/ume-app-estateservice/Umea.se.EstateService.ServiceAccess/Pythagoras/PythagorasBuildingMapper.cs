using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Pythagoras;
using DomainMarkerType = Umea.se.EstateService.Shared.Pythagoras.MarkerType;
using TransportMarkerType = Umea.se.EstateService.ServiceAccess.Pythagoras.Dto.MarkerType;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

internal static class PythagorasBuildingMapper
{
    public static BuildingModel ToDomain(Building dto)
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
            MarkerType = MapMarkerType(dto.MarkerType),
            GeoLocation = MapGeoPoint(dto.GeoLocation),
            Origin = dto.Origin ?? string.Empty,
            PropertyTax = dto.PropertyTax,
            UseWeightsInWorkspaceAreaDistribution = dto.UseWeightsInWorkspaceAreaDistribution
        };
    }

    public static IReadOnlyList<BuildingModel> ToDomain(IReadOnlyList<Building> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return Array.Empty<BuildingModel>();
        }

        BuildingModel[] buffer = new BuildingModel[dtos.Count];
        for (int i = 0; i < dtos.Count; i++)
        {
            buffer[i] = ToDomain(dtos[i]);
        }

        return buffer;
    }

    private static DomainMarkerType MapMarkerType(TransportMarkerType markerType)
    {
        int numeric = (int)markerType;
        if (Enum.IsDefined(typeof(DomainMarkerType), numeric))
        {
            return (DomainMarkerType)numeric;
        }

        return DomainMarkerType.Unknown;
    }

    private static GeoPointModel MapGeoPoint(GeoPoint? dto)
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
