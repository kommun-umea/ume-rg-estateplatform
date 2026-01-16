using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Mappers;

public static class PythagorasEstatePropertyMapper
{
    public static EstateExtendedPropertiesModel? ToExtendedPropertiesModel(IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (properties.Count == 0)
        {
            return null;
        }

        string? operationalArea = TryGetOutputValue(properties, PropertyCategoryId.OperationalArea);
        string? municipalityArea = TryGetOutputValue(properties, PropertyCategoryId.MunicipalityArea);
        string? propertyDesignation = TryGetOutputValue(properties, PropertyCategoryId.PropertyDesignation);

        ExternalOwnerInfoModel? externalOwnerInfo = GetExternalOwnerInfo(properties);

        if (operationalArea is null &&
            municipalityArea is null &&
            propertyDesignation is null &&
            externalOwnerInfo is null)
        {
            return null;
        }

        return new EstateExtendedPropertiesModel
        {
            OperationalArea = operationalArea,
            MunicipalityArea = municipalityArea,
            PropertyDesignation = propertyDesignation,
            ExternalOwnerInfo = externalOwnerInfo,
        };
    }

    public static EstateExtendedPropertiesModel? ToExtendedPropertiesModel(IReadOnlyDictionary<int, PropertyValueDto> properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return null;
        }

        Dictionary<PropertyCategoryId, CalculatedPropertyValueDto> normalized = new(properties.Count);

        foreach ((int key, PropertyValueDto value) in properties)
        {
            if (value?.Value is null)
            {
                continue;
            }

            if (!Enum.IsDefined(typeof(PropertyCategoryId), key))
            {
                continue;
            }

            normalized[(PropertyCategoryId)key] = new CalculatedPropertyValueDto
            {
                OutputValue = value.Value,
                Valid = true
            };
        }

        return normalized.Count == 0
            ? null
            : ToExtendedPropertiesModel(normalized);
    }

    private static ExternalOwnerInfoModel? GetExternalOwnerInfo(IReadOnlyDictionary<PropertyCategoryId, CalculatedPropertyValueDto> properties)
    {

        string? externalStatus = TryGetOutputValue(properties, PropertyCategoryId.EstateExternalStatus);
        string? externalOwnerName = TryGetOutputValue(properties, PropertyCategoryId.EstateExternalOwnerName);
        string? externalOwnerNote = TryGetOutputValue(properties, PropertyCategoryId.EstateExternalOwnerNote);

        if (string.IsNullOrEmpty(externalStatus) &&
             string.IsNullOrEmpty(externalOwnerName) &&
             string.IsNullOrEmpty(externalOwnerNote))
        {
            return null;
        }

        return new ExternalOwnerInfoModel
        {
            Status = externalStatus,
            Name = externalOwnerName,
            Note = externalOwnerNote,
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
}
