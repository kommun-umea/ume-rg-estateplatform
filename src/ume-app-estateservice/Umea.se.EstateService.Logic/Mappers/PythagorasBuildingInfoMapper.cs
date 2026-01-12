using Umea.se.EstateService.Logic.Helpers;
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
            BusinessType = CreateBusinessType(dto),
            SumGrossFloorArea = dto.SumGrossFloorarea ?? 0m,
            NumPlacedPersons = dto.NumPlacedPersons,
            Address = CreateAddress(dto),
            ExtendedProperties = extendedProperties,
        };
    }

    public static IReadOnlyList<BuildingInfoModel> ToModel(IReadOnlyList<BuildingInfo> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Count == 0
            ? []
            : dtos.Select(b => ToModel(b, null)).ToArray();
    }

    private static BusinessTypeModel? CreateBusinessType(BuildingInfo dto)
    {
        if(dto.BusinessTypeId is null || string.IsNullOrWhiteSpace(dto.BusinessTypeName))
        {
            return null;
        }

        return new BusinessTypeModel
        {
            Id = dto.BusinessTypeId.Value,
            Name = dto.BusinessTypeName
        };
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
            StringHelper.Trim(dto.AddressStreet),
            StringHelper.Trim(dto.AddressZipCode),
            StringHelper.Trim(dto.AddressCity),
            StringHelper.Trim(dto.AddressCountry),
            StringHelper.Trim(dto.AddressExtra));
    }

    public static BuildingExtendedPropertiesModel? ToExtendedPropertiesModel(IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (properties.Count == 0)
        {
            return null;
        }

        string? blueprintAvailable = TryGetOutputValue(properties, PropertyCategoryId.BlueprintAvailable);
        string? externalOwner = TryGetOutputValue(properties, PropertyCategoryId.ExternalOwner);
        string? propertyDesignation = TryGetOutputValue(properties, PropertyCategoryId.PropertyDesignation);
        string? yearOfConstruction = TryGetOutputValue(properties, PropertyCategoryId.YearOfConstruction);

        string? noticeBoardText = TryGetOutputValue(properties, PropertyCategoryId.NoticeBoardText);
        BuildingNoticeBoardModel? noticeBoard = null;

        if (!string.IsNullOrEmpty(noticeBoardText))
        {
            DateTime? startDate = DateTime.TryParse(
                    TryGetOutputValue(properties, PropertyCategoryId.NoticeBoardStartDate),
                    out DateTime sd) ? sd : null;

            DateTime? endDate = DateTime.TryParse(
                    TryGetOutputValue(properties, PropertyCategoryId.NoticeBoardEndDate),
                    out DateTime ed) ? ed : null;

            bool isActive = endDate is null || endDate >= DateTime.Today;
            if (isActive)
            {
                noticeBoard = new BuildingNoticeBoardModel
                {
                    Text = noticeBoardText,
                    StartDate = startDate,
                    EndDate = endDate
                };
            }
        }

        bool hasData = blueprintAvailable is not null
            || externalOwner is not null
            || propertyDesignation is not null
            || yearOfConstruction is not null
            || noticeBoard is not null;

        if (!hasData)
        {
            return null;
        }

        return new BuildingExtendedPropertiesModel
        {
            BlueprintAvailable = blueprintAvailable == "Ja",
            ExternalOwner = externalOwner,
            PropertyDesignation = propertyDesignation,
            NoticeBoard = noticeBoard,
            YearOfConstruction = yearOfConstruction
        };
    }

    private static string? TryGetOutputValue(IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto> properties, PropertyCategoryId key)
    {
        if (!properties.TryGetValue(key, out CalculatedPropertyValueDto? value) || value is null)
        {
            return null;
        }

        return value.OutputValue;
    }

    public static BuildingExtendedPropertiesModel? ToExtendedPropertiesModel(IReadOnlyDictionary<int, PropertyValueDto> properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return null;
        }

        Dictionary<PropertyCategoryId, CalculatedPropertyValueDto> normalized = new(properties.Count);

        foreach (KeyValuePair<int, PropertyValueDto> entry in properties)
        {
            if (!Enum.IsDefined(typeof(PropertyCategoryId), entry.Key))
            {
                continue;
            }

            PropertyValueDto property = entry.Value;

            if (property.Value is null)
            {
                continue;
            }

            normalized[(PropertyCategoryId)entry.Key] = new CalculatedPropertyValueDto
            {
                OutputValue = property.Value,
                Valid = true
            };
        }

        return normalized.Count == 0
            ? null
            : ToExtendedPropertiesModel(normalized);
    }
}
