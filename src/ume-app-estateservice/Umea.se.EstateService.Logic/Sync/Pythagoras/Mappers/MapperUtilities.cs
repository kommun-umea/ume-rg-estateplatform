using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;

/// <summary>
/// Shared helper methods used across Pythagoras entity mappers.
/// </summary>
internal static class MapperUtilities
{
    /// <summary>
    /// Attempts to retrieve a property value from a PropertyValues dictionary.
    /// Returns null if propertyValues is null or the property is not found.
    /// </summary>
    internal static string? TryGetPropertyValue(
        Dictionary<int, PropertyValueDto>? propertyValues,
        PropertyCategoryId propertyId)
    {
        if (propertyValues is null)
        {
            return null;
        }

        return propertyValues.TryGetValue((int)propertyId, out PropertyValueDto? propertyDto)
            ? propertyDto.Value
            : null;
    }

    /// <summary>
    /// Creates a GeoPointModel from coordinates.
    /// Returns null if both coordinates are effectively zero.
    /// </summary>
    internal static GeoPointModel? CreateGeoPoint(double x, double y)
    {
        if (Math.Abs(x) < double.Epsilon && Math.Abs(y) < double.Epsilon)
        {
            return null;
        }

        return new GeoPointModel(x, y);
    }

    /// <summary>
    /// Converts a collection of DTOs to entities using the provided mapping function.
    /// </summary>
    internal static List<TEntity> ToEntities<TDto, TEntity>(
        IReadOnlyList<TDto> dtos,
        Func<TDto, TEntity> toEntity)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        List<TEntity> entities = new(dtos.Count);
        foreach (TDto dto in dtos)
        {
            entities.Add(toEntity(dto));
        }

        return entities;
    }
}
