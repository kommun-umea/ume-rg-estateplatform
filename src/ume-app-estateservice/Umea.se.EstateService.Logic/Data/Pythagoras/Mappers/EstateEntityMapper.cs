using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Data.Mappers;

/// <summary>
/// Maps NavigationFolder DTOs from Pythagoras API to EstateEntity objects.
/// </summary>
public static class EstateEntityMapper
{
    /// <summary>
    /// Converts a NavigationFolder DTO to an EstateEntity.
    /// </summary>
    /// <param name="dto">The NavigationFolder DTO from Pythagoras API.</param>
    /// <returns>A mapped EstateEntity.</returns>
    public static EstateEntity ToEntity(NavigationFolder dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new EstateEntity
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name,
            PopularName = dto.PopularName,
            GrossArea = dto.Grossarea ?? 0m,
            NetArea = dto.Netarea ?? 0m,
            GeoLocation = CreateGeoPoint(dto),
            Address = CreateAddress(dto),
            PropertyDesignation = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.PropertyDesignation),
            OperationalArea = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.OperationalArea),
            MunicipalityArea = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.MunicipalityArea),
            BuildingCount = dto.Buildings?.Length ?? 0,
            UpdatedAt = DateTimeOffset.UtcNow, // NavigationFolder doesn't have an updated timestamp
            Buildings = [] // Will be populated during relationship linking
        };
    }

    /// <summary>
    /// Converts a collection of NavigationFolder DTOs to EstateEntity objects.
    /// </summary>
    /// <param name="dtos">The collection of NavigationFolder DTOs.</param>
    /// <returns>A list of mapped EstateEntity objects.</returns>
    public static List<EstateEntity> ToEntities(IReadOnlyList<NavigationFolder> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        List<EstateEntity> entities = new(dtos.Count);
        foreach (NavigationFolder dto in dtos)
        {
            entities.Add(ToEntity(dto));
        }

        return entities;
    }

    /// <summary>
    /// Creates a GeoPointModel from NavigationFolder coordinates.
    /// Returns null if both coordinates are zero.
    /// </summary>
    private static GeoPointModel? CreateGeoPoint(NavigationFolder dto)
    {
        double x = dto.GeoX;
        double y = dto.GeoY;

        if (Math.Abs(x) < double.Epsilon && Math.Abs(y) < double.Epsilon)
        {
            return null;
        }

        return new GeoPointModel(x, y);
    }

    /// <summary>
    /// Creates an AddressModel from NavigationFolder address fields.
    /// Returns null if all address fields are empty.
    /// </summary>
    private static AddressModel? CreateAddress(NavigationFolder dto)
    {
        bool hasValue =
            !string.IsNullOrWhiteSpace(dto.AddressStreet)
            || !string.IsNullOrWhiteSpace(dto.AddressZipCode)
            || !string.IsNullOrWhiteSpace(dto.AddressCity)
            || !string.IsNullOrWhiteSpace(dto.AddressCountry)
            || !string.IsNullOrWhiteSpace(dto.AddressExtra);

        if (!hasValue)
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

    /// <summary>
    /// Attempts to retrieve a property value from the PropertyValues dictionary.
    /// </summary>
    private static string? TryGetPropertyValue(
        Dictionary<int, PropertyValueDto> propertyValues,
        PropertyCategoryId propertyId)
    {
        if (propertyValues.TryGetValue((int)propertyId, out PropertyValueDto? propertyDto))
        {
            return propertyDto.Value;
        }

        return null;
    }
}
