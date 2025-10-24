using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasBuildingInfoMapper
{
    public static BuildingInfoModel ToModel(BuildingInfo dto, BuildingExtendedPropertiesModel? extendedProperties = null)
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
            ExtendedProperties = extendedProperties
        };
    }

    public static IReadOnlyList<BuildingInfoModel> ToModel(IReadOnlyList<BuildingInfo> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(b => ToModel(b, null)).ToArray();
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

    public static BuildingExtendedPropertiesModel? ToExtendedPropertiesModel(IReadOnlyDictionary<BuildingPropertyCategoryId, CalculatedPropertyValueDto> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (properties.Count == 0)
        {
            return null;
        }

        string? externalOwner = TryGetOutputValue(properties, BuildingPropertyCategoryId.ExternalOwner);
        string? propertyDesignation = TryGetOutputValue(properties, BuildingPropertyCategoryId.PropertyDesignation);

        string? noticeBoardText = TryGetOutputValue(properties, BuildingPropertyCategoryId.NoticeBoardText);
        BuildingNoticeBoardModel? noticeBoard = null;

        if (!string.IsNullOrEmpty(noticeBoardText))
        {
            noticeBoard = new BuildingNoticeBoardModel
            {
                Text = noticeBoardText,
                StartDate = DateTime.TryParse(TryGetOutputValue(properties, BuildingPropertyCategoryId.NoticeBoardStartDate), out DateTime startDate) ? startDate : null,
                EndDate = DateTime.TryParse(TryGetOutputValue(properties, BuildingPropertyCategoryId.NoticeBoardEndDate), out DateTime endDate) ? endDate : null
            };
        }

        bool hasData = externalOwner is not null
            || propertyDesignation is not null
            || noticeBoard is not null;

        if (!hasData)
        {
            return null;
        }

        return new BuildingExtendedPropertiesModel
        {
            ExternalOwner = externalOwner,
            PropertyDesignation = propertyDesignation,
            NoticeBoard = noticeBoard
        };
    }

    private static string? TryGetOutputValue(IReadOnlyDictionary<BuildingPropertyCategoryId, CalculatedPropertyValueDto> properties, BuildingPropertyCategoryId key)
    {
        if (!properties.TryGetValue(key, out CalculatedPropertyValueDto? value) || value is null)
        {
            return null;
        }

        return value.OutputValue;
    }
}
