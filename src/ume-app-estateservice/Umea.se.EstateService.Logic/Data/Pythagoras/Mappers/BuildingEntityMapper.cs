using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Data.Mappers;

/// <summary>
/// Maps BuildingInfo DTOs from Pythagoras API to BuildingEntity objects.
/// </summary>
public static class BuildingEntityMapper
{
    /// <summary>
    /// Converts a BuildingInfo DTO to a BuildingEntity.
    /// </summary>
    /// <param name="dto">The BuildingInfo DTO from Pythagoras API.</param>
    /// <param name="estateId">The ID of the estate this building belongs to (optional, can be set later).</param>
    /// <returns>A mapped BuildingEntity.</returns>
    public static BuildingEntity ToEntity(BuildingInfo dto, int estateId = 0)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new BuildingEntity
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.Grossarea ?? 0m,
            NetArea = dto.Netarea ?? 0m,
            GeoLocation = CreateGeoPoint(dto),
            Address = CreateAddress(dto),
            YearOfConstruction = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.YearOfConstruction),
            BuildingCondition = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingCondition),
            ExternalOwnerStatus = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingExternalStatus),
            ExternalOwnerName = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingExternalOwnerName),
            ExternalOwnerNote = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingExternalOwnerNote),
            PropertyDesignation = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.PropertyDesignation),
            NoticeBoard = CreateNoticeBoard(dto.PropertyValues),
            BlueprintAvailable = ParseBlueprintAvailable(dto.PropertyValues),
            ContactPersons = CreateContactPersons(dto.PropertyValues),
            BusinessType = dto.BusinessTypeId is int btId && !string.IsNullOrWhiteSpace(dto.BusinessTypeName)
                ? new BusinessTypeModel { Id = btId, Name = dto.BusinessTypeName }
                : null,
            EstateId = estateId,
            UpdatedAt = DateTimeOffset.UtcNow,
            Floors = [],
            Rooms = []
        };
    }

    /// <summary>
    /// Converts a collection of BuildingInfo DTOs to BuildingEntity objects.
    /// </summary>
    /// <param name="dtos">The collection of BuildingInfo DTOs.</param>
    /// <returns>A list of mapped BuildingEntity objects.</returns>
    public static List<BuildingEntity> ToEntities(IReadOnlyList<BuildingInfo> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        if (dtos.Count == 0)
        {
            return [];
        }

        List<BuildingEntity> entities = new(dtos.Count);
        foreach (BuildingInfo dto in dtos)
        {
            entities.Add(ToEntity(dto));
        }

        return entities;
    }

    /// <summary>
    /// Creates a GeoPointModel from BuildingInfo coordinates.
    /// Returns null if both coordinates are zero.
    /// </summary>
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

    /// <summary>
    /// Creates an AddressModel from BuildingInfo address fields.
    /// Returns null if all address fields are empty.
    /// </summary>
    private static AddressModel? CreateAddress(BuildingInfo dto)
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
    /// Creates a BuildingNoticeBoard from property values.
    /// Returns null if no notice board text is present or if propertyValues is null.
    /// </summary>
    private static BuildingNoticeBoardModel? CreateNoticeBoard(Dictionary<int, PropertyValueDto>? propertyValues)
    {
        if (propertyValues == null)
        {
            return null;
        }

        string? text = TryGetPropertyValue(propertyValues, PropertyCategoryId.NoticeBoardText);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        DateTime? startDate = null;
        DateTime? endDate = null;

        string? startDateStr = TryGetPropertyValue(propertyValues, PropertyCategoryId.NoticeBoardStartDate);
        if (!string.IsNullOrWhiteSpace(startDateStr) && DateTime.TryParse(startDateStr, out DateTime parsedStartDate))
        {
            startDate = parsedStartDate;
        }

        string? endDateStr = TryGetPropertyValue(propertyValues, PropertyCategoryId.NoticeBoardEndDate);
        if (!string.IsNullOrWhiteSpace(endDateStr) && DateTime.TryParse(endDateStr, out DateTime parsedEndDate))
        {
            endDate = parsedEndDate;
        }

        return new BuildingNoticeBoardModel
        {
            Text = text,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    /// <summary>
    /// Parses the BlueprintAvailable property value.
    /// Returns true if value is "Ja", false if "Nej", null otherwise.
    /// </summary>
    private static bool? ParseBlueprintAvailable(Dictionary<int, PropertyValueDto>? propertyValues)
    {
        string? value = TryGetPropertyValue(propertyValues, PropertyCategoryId.BlueprintAvailable);
        return value switch
        {
            "Ja" => true,
            "Nej" => false,
            _ => null
        };
    }

    /// <summary>
    /// Creates a BuildingContactPersonsModel from property values.
    /// Returns null if no contact person data is present.
    /// </summary>
    private static BuildingContactPersonsModel? CreateContactPersons(Dictionary<int, PropertyValueDto>? propertyValues)
    {
        if (propertyValues == null)
        {
            return null;
        }

        string? propertyManager = TryGetPropertyValue(propertyValues, PropertyCategoryId.PropertyManager);
        string? operationsManager = TryGetPropertyValue(propertyValues, PropertyCategoryId.OperationsManager);
        string? operationCoordinator = TryGetPropertyValue(propertyValues, PropertyCategoryId.OperationCoordinator);
        string? rentalAdministrator = TryGetPropertyValue(propertyValues, PropertyCategoryId.RentalAdministrator);

        if (string.IsNullOrEmpty(propertyManager) &&
            string.IsNullOrEmpty(operationsManager) &&
            string.IsNullOrEmpty(operationCoordinator) &&
            string.IsNullOrEmpty(rentalAdministrator))
        {
            return null;
        }

        return new BuildingContactPersonsModel
        {
            PropertyManager = propertyManager ?? string.Empty,
            OperationsManager = operationsManager,
            OperationCoordinator = operationCoordinator,
            RentalAdministrator = rentalAdministrator
        };
    }

    /// <summary>
    /// Attempts to retrieve a property value from the PropertyValues dictionary.
    /// Returns null if propertyValues is null or the property is not found.
    /// </summary>
    private static string? TryGetPropertyValue(
        Dictionary<int, PropertyValueDto>? propertyValues,
        PropertyCategoryId propertyId)
    {
        if (propertyValues == null)
        {
            return null;
        }

        if (propertyValues.TryGetValue((int)propertyId, out PropertyValueDto? propertyDto))
        {
            return propertyDto.Value;
        }

        return null;
    }
}
