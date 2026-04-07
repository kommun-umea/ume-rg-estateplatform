using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Data.Pythagoras.Mappers;
using Umea.se.EstateService.Logic.Handlers;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enums;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Data.Pythagoras;

/// <summary>
/// Service responsible for fetching data from Pythagoras API and updating the in-memory data store.
/// </summary>
public sealed class PythagorasDataRefreshService(
    IPythagorasClient pythagorasClient,
    IDataStore dataStore,
    BuildingBackgroundCache backgroundCache,
    ILogger<PythagorasDataRefreshService> logger) : IDataRefreshService
{
    private static readonly IReadOnlyCollection<int> _buildingExtendedPropertyIds = Array.AsReadOnly(
    [
        (int)PropertyCategoryId.BlueprintAvailable,
        (int)PropertyCategoryId.BuildingCondition,
        (int)PropertyCategoryId.BuildingExternalStatus,
        (int)PropertyCategoryId.BuildingExternalOwnerName,
        (int)PropertyCategoryId.BuildingExternalOwnerNote,
        (int)PropertyCategoryId.PropertyDesignation,
        (int)PropertyCategoryId.NoticeBoardText,
        (int)PropertyCategoryId.NoticeBoardStartDate,
        (int)PropertyCategoryId.NoticeBoardEndDate,
        (int)PropertyCategoryId.YearOfConstruction,
        (int)PropertyCategoryId.PropertyManager,
        (int)PropertyCategoryId.OperationsManager,
        (int)PropertyCategoryId.OperationCoordinator,
        (int)PropertyCategoryId.RentalAdministrator,
        (int)PropertyCategoryId.TownHallServiceOrder,
        (int)PropertyCategoryId.FacilityServiceOrder
    ]);

    // Properties in the "Driftgrupper" category (8) are not cached by Pythagoras,
    // so uilistdata never returns them. We backfill these via the calculatedvalue endpoint.
    // Can be removed if Pythagoras starts caching these properties.
    private static readonly IReadOnlyCollection<int> _uncachedPropertyIds = Array.AsReadOnly(
    [
        (int)PropertyCategoryId.FacilityServiceOrder,
        (int)PropertyCategoryId.PropertyManager,
        (int)PropertyCategoryId.OperationsManager,
        (int)PropertyCategoryId.OperationCoordinator,
        (int)PropertyCategoryId.RentalAdministrator,
        (int)PropertyCategoryId.TownHallServiceOrder
    ]);

    private static readonly IReadOnlyCollection<int> _estateExtendedPropertyIds = Array.AsReadOnly(
    [
        (int)PropertyCategoryId.PropertyDesignation,
        (int)PropertyCategoryId.OperationalArea,
        (int)PropertyCategoryId.AdministrativeArea,
        (int)PropertyCategoryId.MunicipalityArea,
        (int)PropertyCategoryId.EstateExternalStatus,
        (int)PropertyCategoryId.EstateExternalOwnerName,
        (int)PropertyCategoryId.EstateExternalOwnerNote
    ]);

    /// <summary>
    /// NavigationInfo dictionary keys from Pythagoras API.
    /// These correspond to NavigationFolderType IDs returned as string keys.
    /// </summary>
    private static class NavigationInfoKey
    {
        /// <summary>Key for estate name (NavigationFolderType.Estate = 5)</summary>
        public const string Estate = "5";
        /// <summary>Key for district/region name (NavigationFolderType.District = 9)</summary>
        public const string District = "9";
        /// <summary>Key for organization name (NavigationFolderType.ManagementObject = 14)</summary>
        public const string Organization = "14";
    }

    /// <summary>
    /// Holds the raw data fetched from Pythagoras API.
    /// </summary>
    private sealed class PythagorasData
    {
        public required IReadOnlyList<NavigationFolder> Estates { get; init; }
        /// <summary>
        /// Districts (typeId=9) for building-to-region name lookup.
        /// </summary>
        public required IReadOnlyList<NavigationFolder> Districts { get; init; }
        /// <summary>
        /// ManagementObjects (typeId=14) for building-to-organization name lookup.
        /// </summary>
        public required IReadOnlyList<NavigationFolder> Organizations { get; init; }
        /// <summary>
        /// Buildings with NavigationInfo populated (includeNavigationInfo=true).
        /// NavigationInfo contains folder names keyed by type ID (e.g., "5" -> estate name).
        /// </summary>
        public required IReadOnlyList<BuildingInfo> Buildings { get; init; }
        public required IReadOnlyList<Floor> Floors { get; init; }
        public required IReadOnlyList<Workspace> Workspaces { get; init; }
        public required IReadOnlyList<WorkOrderCategoryInfoDto> WorkOrderCategories { get; init; }
    }

    /// <inheritdoc />
    public async Task RefreshDataAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        try
        {
            logger.LogInformation("Starting data refresh from Pythagoras API");

            // Record the refresh attempt
            dataStore.RecordRefreshAttempt(startTime);

            PythagorasData data = await FetchDataSequentiallyAsync(cancellationToken).ConfigureAwait(false);

            List<EstateEntity> estates = EstateEntityMapper.ToEntities(data.Estates);
            List<BuildingEntity> buildings = BuildingEntityMapper.ToEntities(data.Buildings);
            List<FloorEntity> floors = FloorEntityMapper.ToEntities(data.Floors);
            List<RoomEntity> rooms = RoomEntityMapper.ToEntities(data.Workspaces);

            // Resolve building-to-estate relationships using NavigationInfo
            Dictionary<int, int> buildingToEstate = BuildBuildingToEstateLookupByName(data.Buildings, data.Estates);

            int linkedCount = 0;
            foreach (BuildingEntity building in buildings)
            {
                if (buildingToEstate.TryGetValue(building.Id, out int estateId))
                {
                    building.EstateId = estateId;
                    linkedCount++;
                }
            }

            if (linkedCount < buildings.Count)
            {
                logger.LogWarning("{UnlinkedCount} buildings have no estate parent",
                    buildings.Count - linkedCount);
            }

            BuildEntityRelationships(estates, buildings, floors, rooms);
            CalculateBuildingWorkspaceStats(buildings);

            // Write cached image IDs + document counts onto the new building entities before snapshot creation
            backgroundCache.ApplyTo(buildings);

            // Build ascendant lookup using NavigationInfo from buildings
            Dictionary<int, BuildingAscendantTriplet> buildingAscendants = BuildAscendantLookupByName(
                data.Buildings, data.Estates, data.Districts, data.Organizations);

            // Map work order categories
            ImmutableArray<WorkOrderCategoryNode> workOrderCategories = [.. data.WorkOrderCategories.Select(dto => new WorkOrderCategoryNode
            {
                Id = dto.Id,
                ParentId = dto.ParentId,
                Name = dto.Name ?? string.Empty,
                WorkOrderTypeIds = dto.WorkOrderTypeIds ?? []
            })];

            DateTimeOffset refreshTime = DateTimeOffset.UtcNow;
            DataSnapshot snapshot = new(
                estates: [.. estates],
                buildings: [.. buildings],
                floors: [.. floors],
                rooms: [.. rooms],
                buildingAscendants: buildingAscendants,
                refreshUtc: refreshTime,
                workOrderCategories: workOrderCategories
            );

            dataStore.SetSnapshot(snapshot);

            double duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            logger.LogInformation(
                "Data refresh completed successfully in {Duration}ms. " +
                "Estates: {EstateCount}, Buildings: {BuildingCount}, Floors: {FloorCount}, " +
                "Rooms: {RoomCount}, Ascendants: {AscendantCount}, WorkOrderCategories: {CategoryCount}",
                duration, estates.Count, buildings.Count, floors.Count, rooms.Count,
                buildingAscendants.Count, workOrderCategories.Length);
        }
        catch (Exception ex)
        {
            double duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            if (ex is HttpRequestException httpEx)
            {
                object? responseBody = httpEx.StatusCode == System.Net.HttpStatusCode.InternalServerError
                    && httpEx.Data.Contains("ResponseBody") ? httpEx.Data["ResponseBody"] : null;

                logger.LogError(httpEx,
                    "Data refresh failed with HTTP {StatusCode} after {Duration}ms. Message: {Message}, Response: {Response}",
                    httpEx.StatusCode,
                    duration,
                    httpEx.Message,
                    responseBody ?? "N/A");
            }
            else
            {
                logger.LogError(ex, "Data refresh failed after {Duration}ms.", duration);
            }

            throw;
        }
    }

    /// <summary>
    /// Builds a lookup dictionary mapping building IDs to their estate IDs.
    /// Uses NavigationInfo from buildings (key "5" = estate name) and matches to estate entities by name.
    /// </summary>
    private static Dictionary<int, int> BuildBuildingToEstateLookupByName(
        IReadOnlyList<BuildingInfo> buildings,
        IReadOnlyList<NavigationFolder> estates)
    {
        // Build name -> estate ID lookup (names should be unique within type; take first on collision)
        Dictionary<string, int> estateIdByName = estates
            .Where(e => e.NavigationId == NavigationType.UmeaKommun)
            .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        Dictionary<int, int> buildingToEstate = [];

        foreach (BuildingInfo building in buildings)
        {
            if (building.NavigationInfo.TryGetValue(NavigationInfoKey.Estate, out string? estateName) &&
                !string.IsNullOrEmpty(estateName) &&
                estateIdByName.TryGetValue(estateName, out int estateId))
            {
                buildingToEstate[building.Id] = estateId;
            }
        }

        return buildingToEstate;
    }

    /// <summary>
    /// Builds hierarchical relationships between entities.
    /// </summary>
    private void BuildEntityRelationships(List<EstateEntity> estates, List<BuildingEntity> buildings, List<FloorEntity> floors, List<RoomEntity> rooms)
    {
        // Create lookup dictionaries
        Dictionary<int, EstateEntity> estatesById = estates.ToDictionary(e => e.Id);
        Dictionary<int, BuildingEntity> buildingsById = buildings.ToDictionary(b => b.Id);
        Dictionary<int, FloorEntity> floorsById = floors.ToDictionary(f => f.Id);

        // Link buildings to estates
        foreach (BuildingEntity building in buildings)
        {
            if (building.EstateId > 0 && estatesById.TryGetValue(building.EstateId, out EstateEntity? estate))
            {
                estate.Buildings.Add(building);
            }
        }

        // Link floors to buildings
        foreach (FloorEntity floor in floors)
        {
            if (floor.BuildingId > 0 && buildingsById.TryGetValue(floor.BuildingId, out BuildingEntity? building))
            {
                building.Floors.Add(floor);
            }
        }

        // Link rooms to buildings and floors
        foreach (RoomEntity room in rooms)
        {
            if (room.BuildingId > 0 && buildingsById.TryGetValue(room.BuildingId, out BuildingEntity? building))
            {
                building.Rooms.Add(room);
            }

            if (room.FloorId.HasValue && floorsById.TryGetValue(room.FloorId.Value, out FloorEntity? floor))
            {
                floor.Rooms.Add(room);
            }
        }

        // Update building counts
        foreach (EstateEntity estate in estates)
        {
            estate.BuildingCount = estate.Buildings.Count;
        }
    }

    private static void CalculateBuildingWorkspaceStats(List<BuildingEntity> buildings)
    {
        foreach (BuildingEntity building in buildings)
        {
            building.NumFloors = building.Floors.Count;
            building.NumRooms = building.Rooms.Count;
        }
    }

    /// <summary>
    /// Creates a query for fetching folders of a specific type.
    /// Used to get folder entities for name-based lookup from building NavigationInfo.
    /// </summary>
    private static PythagorasQuery<NavigationFolder> CreateFolderQuery(int folderTypeId)
    {
        return new PythagorasQuery<NavigationFolder>()
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun)
            .WithQueryParameter("typeId", folderTypeId);
    }

    /// <summary>
    /// Creates a BuildingAscendantModel from a NavigationFolder DTO.
    /// </summary>
    private static BuildingAscendantModel CreateAscendantFromFolder(
        NavigationFolder folder,
        BuildingAscendantType type)
    {
        return new BuildingAscendantModel
        {
            Id = folder.Id,
            Name = folder.Name,
            PopularName = folder.PopularName,
            GeoLocation = folder.GeoX != 0 && folder.GeoY != 0
                ? new GeoPointModel(folder.GeoX, folder.GeoY)
                : null,
            Type = type
        };
    }

    /// <summary>
    /// Builds a dictionary mapping building IDs to their ascendant triplets.
    /// Uses NavigationInfo from buildings to match folder names to folder entities.
    /// NavigationInfo keys: "5" = estate, "9" = district, "14" = organization.
    /// </summary>
    private Dictionary<int, BuildingAscendantTriplet> BuildAscendantLookupByName(
        IReadOnlyList<BuildingInfo> buildings,
        IReadOnlyList<NavigationFolder> estates,
        IReadOnlyList<NavigationFolder> districts,
        IReadOnlyList<NavigationFolder> organizations)
    {
        // Build name -> folder lookups (names should be unique within each type; take first on collision)
        Dictionary<string, NavigationFolder> estatesByName = estates
            .Where(f => f.NavigationId == NavigationType.UmeaKommun)
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Dictionary<string, NavigationFolder> districtsByName = districts
            .Where(f => f.NavigationId == NavigationType.UmeaKommun)
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Dictionary<string, NavigationFolder> organizationsByName = organizations
            .Where(f => f.NavigationId == NavigationType.UmeaKommun)
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        Dictionary<int, BuildingAscendantTriplet> result = [];

        foreach (BuildingInfo building in buildings)
        {
            BuildingAscendantModel? estate = null;
            BuildingAscendantModel? region = null;
            BuildingAscendantModel? organization = null;

            if (building.NavigationInfo.TryGetValue(NavigationInfoKey.Estate, out string? estateName) &&
                !string.IsNullOrEmpty(estateName) &&
                estatesByName.TryGetValue(estateName, out NavigationFolder? estateFolder))
            {
                estate = CreateAscendantFromFolder(estateFolder, BuildingAscendantType.Estate);
            }

            if (building.NavigationInfo.TryGetValue(NavigationInfoKey.District, out string? districtName) &&
                !string.IsNullOrEmpty(districtName) &&
                districtsByName.TryGetValue(districtName, out NavigationFolder? districtFolder))
            {
                region = CreateAscendantFromFolder(districtFolder, BuildingAscendantType.Area);
            }

            if (building.NavigationInfo.TryGetValue(NavigationInfoKey.Organization, out string? orgName) &&
                !string.IsNullOrEmpty(orgName) &&
                organizationsByName.TryGetValue(orgName, out NavigationFolder? orgFolder))
            {
                organization = CreateAscendantFromFolder(orgFolder, BuildingAscendantType.Organization);
            }

            result[building.Id] = new BuildingAscendantTriplet
            {
                Estate = estate,
                Region = region,
                Organization = organization
            };
        }

        return result;
    }

    private async Task<PythagorasData> FetchDataSequentiallyAsync(CancellationToken cancellationToken)
    {
        NavigationFolderUiListDataRequest estateRequest = new()
        {
            NavigationFolderIds = null,
            PropertyIds = _estateExtendedPropertyIds,
            NavigationId = NavigationType.UmeaKommun,
            IncludePropertyValues = true,
            IncludeAscendantBuildings = false
        };

        UiListDataResponse<NavigationFolder> estateResponse = await pythagorasClient
            .PostNavigationFolderUiListDataAsync(estateRequest, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<NavigationFolder> districts = await pythagorasClient
            .GetNavigationFoldersAsync(query: CreateFolderQuery(NavigationFolderType.District), cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<NavigationFolder> organizations = await pythagorasClient
            .GetNavigationFoldersAsync(query: CreateFolderQuery(NavigationFolderType.ManagementObject), cancellationToken)
            .ConfigureAwait(false);

        BuildingUiListDataRequest buildingRequest = new()
        {
            BuildingIds = null,
            PropertyIds = _buildingExtendedPropertyIds,
            NavigationId = NavigationType.UmeaKommun,
            IncludePropertyValues = true,
            IncludeNavigationInfo = true
        };

        UiListDataResponse<BuildingInfo> buildingResponse = await pythagorasClient
            .PostBuildingUiListDataAsync(buildingRequest, cancellationToken)
            .ConfigureAwait(false);

        // Pythagoras uilistdata only returns properties that have cached values.
        // Some properties (e.g. "Driftgrupper" category: contact persons, service order flags)
        // may not have cached values despite having valid calculated values.
        // Until Pythagoras rebuilds its property cache, we fall back to the per-building
        // calculated property value endpoint for any properties missing from uilistdata.
        await BackfillMissingPropertyValuesAsync(buildingResponse.Data, _uncachedPropertyIds, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Workspace> workspaces = await pythagorasClient
            .GetWorkspacesAsync(query: null, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Floor> floors = await FetchFloorsViaWorkspaceBatchesAsync(workspaces, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<WorkOrderCategoryInfoDto> workOrderCategories = await pythagorasClient
            .GetWorkOrderCategoriesAsync(1, cancellationToken)
            .ConfigureAwait(false);

        return new PythagorasData
        {
            Estates = estateResponse.Data,
            Districts = districts,
            Organizations = organizations,
            Buildings = buildingResponse.Data,
            Floors = floors,
            Workspaces = workspaces,
            WorkOrderCategories = workOrderCategories
        };
    }

    /// <summary>
    /// For properties that uilistdata did not return (no cached value in Pythagoras),
    /// falls back to the calculated property value endpoint per building.
    /// </summary>
    private async Task BackfillMissingPropertyValuesAsync(
        IReadOnlyList<BuildingInfo> buildings,
        IReadOnlyCollection<int> requestedPropertyIds,
        CancellationToken cancellationToken)
    {
        List<(BuildingInfo Building, int[] MissingIds)> buildingsWithMissing = [];

        foreach (BuildingInfo building in buildings)
        {
            int[] missing = requestedPropertyIds
                .Where(pid => !building.PropertyValues.ContainsKey(pid))
                .ToArray();

            if (missing.Length > 0)
            {
                buildingsWithMissing.Add((building, missing));
            }
        }

        if (buildingsWithMissing.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Backfilling calculated property values for {Count} buildings with missing cached properties",
            buildingsWithMissing.Count);

        foreach ((BuildingInfo building, int[] missingIds) in buildingsWithMissing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                CalculatedPropertyValueRequest request = new()
                {
                    PropertyIds = missingIds,
                    NavigationId = NavigationType.UmeaKommun
                };

                IReadOnlyDictionary<int, CalculatedPropertyValueDto> calculated = await pythagorasClient
                    .GetBuildingCalculatedPropertyValuesAsync(building.Id, request, cancellationToken)
                    .ConfigureAwait(false);

                foreach (KeyValuePair<int, CalculatedPropertyValueDto> kvp in calculated)
                {
                    if (kvp.Value.OutputValue is not null)
                    {
                        building.PropertyValues[kvp.Key] = new PropertyValueDto { Value = kvp.Value.OutputValue };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to backfill calculated properties for building {BuildingId}, skipping",
                    building.Id);
            }
        }
    }

    private async Task<IReadOnlyList<Floor>> FetchFloorsViaWorkspaceBatchesAsync(
        IReadOnlyList<Workspace> workspaces,
        CancellationToken cancellationToken)
    {
        const int floorBatchSize = 100;

        HashSet<int> floorIdsFromWorkspaces = [];

        foreach (Workspace workspace in workspaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (workspace.FloorId is int floorId && floorId > 0)
            {
                floorIdsFromWorkspaces.Add(floorId);
            }
        }

        Dictionary<int, Floor> floorsById = [];

        if (floorIdsFromWorkspaces.Count > 0)
        {
            foreach (int[] batch in floorIdsFromWorkspaces.Chunk(floorBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                PythagorasQuery<Floor> query = new PythagorasQuery<Floor>()
                    .WithQueryParameterValues("floorIds[]", batch);

                IReadOnlyList<Floor> batchFloors = await pythagorasClient
                    .GetFloorsAsync(query, cancellationToken)
                    .ConfigureAwait(false);

                foreach (Floor floor in batchFloors)
                {
                    floorsById[floor.Id] = floor;
                }
            }
        }
        else
        {
            logger.LogInformation("No floor ids could be derived from workspace data");
        }

        return [.. floorsById.Values];
    }
}
