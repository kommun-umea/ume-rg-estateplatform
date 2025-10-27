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

        string? operatingArea = TryGetOutputValue(properties, PropertyCategoryId.OperatingArea);
        string? municipalityArea = TryGetOutputValue(properties, PropertyCategoryId.MunicipalityArea);
        string? propertyDesignation = TryGetOutputValue(properties, PropertyCategoryId.PropertyDesignation);

        if (operatingArea is null && municipalityArea is null && propertyDesignation is null)
        {
            return null;
        }

        return new EstateExtendedPropertiesModel
        {
            OperatingArea = operatingArea,
            MunicipalityArea = municipalityArea,
            PropertyDesignation = propertyDesignation
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
