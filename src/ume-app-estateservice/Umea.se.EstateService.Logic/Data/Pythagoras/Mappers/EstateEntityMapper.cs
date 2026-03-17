using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.ValueObjects;
using static Umea.se.EstateService.Logic.Data.Pythagoras.Mappers.MapperUtilities;

namespace Umea.se.EstateService.Logic.Data.Pythagoras.Mappers;

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
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.Grossarea ?? 0m,
            NetArea = dto.Netarea ?? 0m,
            GeoLocation = CreateGeoPoint(dto.GeoX, dto.GeoY),
            Address = CreateAddress(dto),
            PropertyDesignation = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.PropertyDesignation),
            OperationalArea = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.OperationalArea),
            AdministrativeArea = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.AdministrativeArea),
            MunicipalityArea = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.MunicipalityArea),
            ExternalOwnerStatus = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.EstateExternalStatus),
            ExternalOwnerName = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.EstateExternalOwnerName),
            ExternalOwnerNote = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.EstateExternalOwnerNote),
            BuildingCount = dto.Buildings?.Length ?? 0,
            UpdatedAt = DateTimeOffset.UtcNow, // NavigationFolder doesn't have an updated timestamp
            Buildings = [] // Will be populated during relationship linking
        };
    }

    /// <summary>
    /// Converts a collection of NavigationFolder DTOs to EstateEntity objects.
    /// </summary>
    public static List<EstateEntity> ToEntities(IReadOnlyList<NavigationFolder> dtos)
        => MapperUtilities.ToEntities(dtos, ToEntity);

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

}
