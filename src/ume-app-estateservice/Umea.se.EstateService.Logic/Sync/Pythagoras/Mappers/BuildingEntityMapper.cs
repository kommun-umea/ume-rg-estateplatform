using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Parsing;
using Umea.se.EstateService.Shared.ValueObjects;
using static Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers.MapperUtilities;

namespace Umea.se.EstateService.Logic.Sync.Pythagoras.Mappers;

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

        BuildingContactPersonsModel? contactPersons = CreateContactPersons(dto.PropertyValues);

        return new BuildingEntity
        {
            Id = dto.Id,
            Uid = dto.Uid,
            Name = dto.Name ?? string.Empty,
            PopularName = dto.PopularName ?? string.Empty,
            GrossArea = dto.Grossarea ?? 0m,
            NetArea = dto.Netarea ?? 0m,
            GeoLocation = CreateGeoPoint(dto.GeoX, dto.GeoY),
            Address = CreateAddress(dto),
            YearOfConstruction = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.YearOfConstruction),
            BuildingCondition = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingCondition),
            ExternalOwnerStatus = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingExternalStatus),
            ExternalOwnerName = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingExternalOwnerName),
            ExternalOwnerNote = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.BuildingExternalOwnerNote),
            PropertyDesignation = TryGetPropertyValue(dto.PropertyValues, PropertyCategoryId.PropertyDesignation),
            NoticeBoard = CreateNoticeBoard(dto.PropertyValues),
            BlueprintAvailable = ParseBlueprintAvailable(dto.PropertyValues),
            ContactPersons = contactPersons,
            LegacyPropertyManager = contactPersons?.PropertyManager.Name ?? string.Empty,
            LegacyOperationsManager = contactPersons?.OperationsManager?.Name,
            LegacyOperationCoordinator = contactPersons?.OperationCoordinator?.Name,
            LegacyRentalAdministrator = contactPersons?.RentalAdministrator?.Name,
            WorkOrderTypes = BuildWorkOrderTypes(dto.PropertyValues),
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
    public static List<BuildingEntity> ToEntities(IReadOnlyList<BuildingInfo> dtos) => MapperUtilities.ToEntities(dtos, dto => ToEntity(dto));

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
    /// Name properties (282-285) drive presence. Contact properties (306-309) hold "phone / email"
    /// text from Pythagoras and are parsed leniently via ContactInfoParser.
    /// Returns null if no name property is populated.
    /// </summary>
    private static BuildingContactPersonsModel? CreateContactPersons(Dictionary<int, PropertyValueDto>? propertyValues)
    {
        if (propertyValues == null)
        {
            return null;
        }

        BuildingContactModel? propertyManager = BuildContact(propertyValues, PropertyCategoryId.PropertyManager, PropertyCategoryId.PropertyManagerContact);
        BuildingContactModel? operationsManager = BuildContact(propertyValues, PropertyCategoryId.OperationsManager, PropertyCategoryId.OperationsManagerContact);
        BuildingContactModel? operationCoordinator = BuildContact(propertyValues, PropertyCategoryId.OperationCoordinator, PropertyCategoryId.OperationCoordinatorContact);
        BuildingContactModel? rentalAdministrator = BuildContact(propertyValues, PropertyCategoryId.RentalAdministrator, PropertyCategoryId.RentalAdministratorContact);

        if (propertyManager is null &&
            operationsManager is null &&
            operationCoordinator is null &&
            rentalAdministrator is null)
        {
            return null;
        }

        // PropertyManager is required on the model. If only other roles are populated, synthesize
        // an empty-name PropertyManager to keep the model constructible without losing data.
        propertyManager ??= new BuildingContactModel { Name = string.Empty };

        return new BuildingContactPersonsModel
        {
            PropertyManager = propertyManager,
            OperationsManager = operationsManager,
            OperationCoordinator = operationCoordinator,
            RentalAdministrator = rentalAdministrator
        };
    }

    private static BuildingContactModel? BuildContact(
        Dictionary<int, PropertyValueDto> propertyValues,
        PropertyCategoryId nameProperty,
        PropertyCategoryId contactProperty)
    {
        string? name = TryGetPropertyValue(propertyValues, nameProperty);
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        string? raw = TryGetPropertyValue(propertyValues, contactProperty);
        (string? phone, string? email) = ContactInfoParser.Parse(raw);

        return new BuildingContactModel
        {
            Name = name,
            Phone = phone,
            Email = email
        };
    }

    private static List<WorkOrderType> BuildWorkOrderTypes(Dictionary<int, PropertyValueDto>? propertyValues)
    {
        string? externalStatus = TryGetPropertyValue(propertyValues, PropertyCategoryId.BuildingExternalStatus);

        if (!string.IsNullOrEmpty(externalStatus) && externalStatus != "Egen")
        {
            return [];
        }

        List<WorkOrderType> types = [WorkOrderType.ErrorReport, WorkOrderType.BuildingService];

        if (TryGetPropertyValue(propertyValues, PropertyCategoryId.TownHallServiceOrder) == "Ja")
        {
            types.Add(WorkOrderType.TownHallService);
        }

        if (TryGetPropertyValue(propertyValues, PropertyCategoryId.FacilityServiceOrder) == "Ja")
        {
            types.Add(WorkOrderType.FacilityService);
        }

        return types;
    }

}
