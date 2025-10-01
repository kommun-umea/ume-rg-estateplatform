using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasBuildingInfoMapper
{
    public static BuildingInfoModel ToModel(BuildingInfo dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuildingInfoModel
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GeoLocation = CreateGeoPoint(dto),
            GrossArea = dto.Grossarea ?? 0m,
            NetArea = dto.Netarea ?? 0m,
            SumGrossFloorArea = dto.SumGrossFloorarea ?? 0m,
            NumPlacedPersons = dto.NumPlacedPersons,
            Address = CreateAddress(dto),
            ExtraInfo = ToDictionary(dto.ExtraInfo),
            PropertyValues = ToDictionary(dto.PropertyValues),
            NavigationInfo = ToDictionary(dto.NavigationInfo)
        };
    }

    public static IReadOnlyList<BuildingInfoModel> ToModel(IReadOnlyList<BuildingInfo> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? Array.Empty<BuildingInfoModel>()
            : dtos.Select(ToModel).ToArray();
    }

    private static GeoPointModel? CreateGeoPoint(BuildingInfo dto)
    {
        double x = dto.GeoX;
        double y = dto.GeoY;

        if (Math.Abs(x) < double.Epsilon && Math.Abs(y) < double.Epsilon)
        {
            return null;
        }

        return new GeoPointModel(x, y);
    }

    private static AddressModel? CreateAddress(BuildingInfo dto)
    {
        bool hasAddress = !string.IsNullOrWhiteSpace(dto.AddressStreet)
            || !string.IsNullOrWhiteSpace(dto.AddressZipCode)
            || !string.IsNullOrWhiteSpace(dto.AddressCity)
            || !string.IsNullOrWhiteSpace(dto.AddressCountry)
            || !string.IsNullOrWhiteSpace(dto.AddressExtra);

        if (!hasAddress)
        {
            return null;
        }

        return new AddressModel(
            dto.AddressStreet ?? string.Empty,
            dto.AddressZipCode ?? string.Empty,
            dto.AddressCity ?? string.Empty,
            dto.AddressCountry ?? string.Empty,
            dto.AddressExtra ?? string.Empty);
    }

    private static IReadOnlyDictionary<string, string?> ToDictionary(Dictionary<string, string?>? source)
    {
        if (source is null || source.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        return source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }
}
