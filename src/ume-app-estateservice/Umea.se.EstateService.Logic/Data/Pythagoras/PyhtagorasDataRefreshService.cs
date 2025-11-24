using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.Logic.Data.Mappers;
using Umea.se.EstateService.ServiceAccess.Data;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;

namespace Umea.se.EstateService.Logic.Data.Pythagoras;

/// <summary>
/// Service responsible for fetching data from Pythagoras API and updating the in-memory data store.
/// </summary>
public sealed class PythagorasDataRefreshService(IPythagorasClient pythagorasClient, IDataStore dataStore, ILogger<PythagorasDataRefreshService> logger) : IDataRefreshService
{
    /// <summary>
    /// Holds the raw data fetched from Pythagoras API.
    /// </summary>
    private sealed class PythagorasData
    {
        public required IReadOnlyList<NavigationFolder> Estates { get; init; }
        public required IReadOnlyList<BuildingInfo> Buildings { get; init; }
        public required IReadOnlyList<Floor> Floors { get; init; }
        public required IReadOnlyList<Workspace> Workspaces { get; init; }
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

            // Step 1: Fetch all data
            PythagorasData data = await FetchDataAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Fetched {EstateCount} estates, {BuildingCount} buildings, {FloorCount} floors, {WorkspaceCount} workspaces",
                data.Estates.Count, data.Buildings.Count, data.Floors.Count, data.Workspaces.Count);

            // Step 2: Map DTOs to entities
            logger.LogDebug("Mapping DTOs to entities");

            List<EstateEntity> estates = EstateEntityMapper.ToEntities(data.Estates);
            List<BuildingEntity> buildings = BuildingEntityMapper.ToEntities(data.Buildings);
            List<FloorEntity> floors = FloorEntityMapper.ToEntities(data.Floors);
            List<RoomEntity> rooms = RoomEntityMapper.ToEntities(data.Workspaces);

            // Step 3: Resolve building-to-estate relationships
            logger.LogDebug("Resolving building-to-estate relationships");
            Dictionary<int, int> buildingToEstate = BuildBuildingToEstateLookup(data.Estates);

            int linkedCount = 0;
            foreach (BuildingEntity building in buildings)
            {
                if (buildingToEstate.TryGetValue(building.Id, out int estateId))
                {
                    building.EstateId = estateId;
                    linkedCount++;
                }
            }

            logger.LogDebug("Set EstateId for {LinkedCount} of {TotalCount} buildings",
                linkedCount, buildings.Count);

            if (linkedCount < buildings.Count)
            {
                logger.LogWarning("{UnlinkedCount} buildings have no estate parent",
                    buildings.Count - linkedCount);
            }

            // Step 4: Build hierarchical relationships
            logger.LogDebug("Building entity relationships");
            BuildEntityRelationships(estates, buildings, floors, rooms);

            // Step 5: Calculate building workspace statistics
            logger.LogDebug("Calculating building workspace statistics");
            CalculateBuildingWorkspaceStats(buildings);

            // Step 6: Create immutable snapshots and update data store
            logger.LogDebug("Creating immutable snapshots and updating data store");

            ImmutableArray<EstateEntity> estateSnapshot = [.. estates];
            ImmutableArray<BuildingEntity> buildingSnapshot = [.. buildings];
            ImmutableArray<FloorEntity> floorSnapshot = [.. floors];
            ImmutableArray<RoomEntity> roomSnapshot = [.. rooms];

            DateTimeOffset refreshTime = DateTimeOffset.UtcNow;
            dataStore.ReplaceSnapshots(estateSnapshot, buildingSnapshot, floorSnapshot, roomSnapshot, refreshTime);

            double duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            logger.LogInformation(
                "Data refresh completed successfully in {Duration}ms. " +
                "Estates: {EstateCount}, Buildings: {BuildingCount}, Floors: {FloorCount}, Rooms: {RoomCount}",
                duration, estates.Count, buildings.Count, floors.Count, rooms.Count);
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.InternalServerError)
        {
            double duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            logger.LogError(httpEx,
                "Data refresh failed with HTTP 500 Internal Server Error after {Duration}ms. " +
                "StatusCode: {StatusCode}, Message: {Message}, Response: {Response}",
                duration,
                httpEx.StatusCode,
                httpEx.Message,
                httpEx.Data.Contains("ResponseBody") ? httpEx.Data["ResponseBody"] : "No response body available");
            throw;
        }
        catch (HttpRequestException httpEx)
        {
            double duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            logger.LogError(httpEx,
                "Data refresh failed with HTTP error after {Duration}ms. " +
                "StatusCode: {StatusCode}, Message: {Message}",
                duration,
                httpEx.StatusCode,
                httpEx.Message);
            throw;
        }
        catch (Exception ex)
        {
            double duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            logger.LogError(ex,
                "Data refresh failed after {Duration}ms. Exception type: {ExceptionType}, Message: {Message}",
                duration,
                ex.GetType().Name,
                ex.Message);
            throw;
        }
    }

    private async Task<PythagorasData> FetchDataAsync(CancellationToken cancellationToken)
    {
        PythagorasData data = await FetchDataSequentiallyAsync(cancellationToken).ConfigureAwait(false);

        return data;
    }

    /// <summary>
    /// Builds a lookup dictionary mapping building IDs to their estate IDs.
    /// Extracts the relationships from the estate DTOs that include their buildings.
    /// </summary>
    private static Dictionary<int, int> BuildBuildingToEstateLookup(IReadOnlyList<NavigationFolder> estatesWithBuildings)
    {
        Dictionary<int, int> buildingToEstate = [];

        foreach (NavigationFolder estate in estatesWithBuildings)
        {
            if (estate.Buildings is null || estate.Buildings.Length == 0)
            {
                continue;
            }

            foreach (Building building in estate.Buildings)
            {
                buildingToEstate[building.Id] = estate.Id;
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

    private void CalculateBuildingWorkspaceStats(List<BuildingEntity> buildings)
    {
        foreach (BuildingEntity building in buildings)
        {
            building.NumFloors = building.Floors.Count;
            building.NumRooms = building.Rooms.Count;
        }

        logger.LogDebug(
            "Building workspace stats calculated: {BuildingCount} buildings with {TotalFloors} floors and {TotalRooms} rooms",
            buildings.Count,
            buildings.Sum(b => b.NumFloors),
            buildings.Sum(b => b.NumRooms));
    }

    /// <summary>
    /// Creates a query for fetching estates (NavigationFolders that are real estates).
    /// </summary>
    private static PythagorasQuery<NavigationFolder> CreateEstateQuery()
    {
        // Filter to only get real estates with buildings included
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .Where(f => f.TypeId, NavigationFolderType.Estate)
            .WithQueryParameter("navigationId", NavigationType.UmeaKommun)
            .WithQueryParameter("includeAscendantBuildings", true);

        return query;
    }

    /// <summary>
    /// Fetches all data from Pythagoras API sequentially.
    /// Use this method when debugging or when the API has issues with parallel requests.
    /// </summary>
    private async Task<PythagorasData> FetchDataSequentiallyAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching data from Pythagoras API sequentially");

        logger.LogDebug("Fetching estates...");
        IReadOnlyList<NavigationFolder> estates = await pythagorasClient.GetNavigationFoldersAsync(query: CreateEstateQuery(), cancellationToken).ConfigureAwait(false);
        logger.LogDebug("{Count} estates", estates.Count);

        logger.LogDebug("Fetching buildings...");
        IReadOnlyList<BuildingInfo> buildings = await pythagorasClient.GetBuildingsAsync(query: null, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Fetched {Count} buildings", buildings.Count);

        logger.LogDebug("Fetching workspaces...");
        IReadOnlyList<Workspace> workspaces = await pythagorasClient.GetWorkspacesAsync(query: null, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Fetched {Count} workspaces", workspaces.Count);

        IReadOnlyList<Floor> floors = await FetchFloorsViaWorkspaceBatchesAsync(workspaces, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Fetched {Count} floors via workspace-derived batching", floors.Count);

        return new PythagorasData
        {
            Estates = estates,
            Buildings = buildings,
            Floors = floors,
            Workspaces = workspaces
        };
    }

    /// <summary>
    /// Fetches all data from Pythagoras API in parallel.
    /// Use this method for optimal performance when the API can handle concurrent requests.
    /// </summary>
#pragma warning disable IDE0051 // Remove unused private members
    private async Task<PythagorasData> FetchDataInParallelAsync(CancellationToken cancellationToken)
#pragma warning restore IDE0051 // Remove unused private members
    {
        logger.LogDebug("Fetching data from Pythagoras API in parallel");

        Task<IReadOnlyList<NavigationFolder>> estatesTask = pythagorasClient.GetNavigationFoldersAsync(query: CreateEstateQuery(), cancellationToken);
        Task<IReadOnlyList<BuildingInfo>> buildingsTask = pythagorasClient.GetBuildingsAsync(query: null, cancellationToken);
        Task<IReadOnlyList<Workspace>> workspacesTask = pythagorasClient.GetWorkspacesAsync(query: null, cancellationToken);

        await Task.WhenAll(estatesTask, buildingsTask, workspacesTask).ConfigureAwait(false);

        IReadOnlyList<NavigationFolder> estates = await estatesTask.ConfigureAwait(false);
        IReadOnlyList<BuildingInfo> buildings = await buildingsTask.ConfigureAwait(false);
        IReadOnlyList<Workspace> workspaces = await workspacesTask.ConfigureAwait(false);

        IReadOnlyList<Floor> floors = await FetchFloorsViaWorkspaceBatchesAsync(workspaces, cancellationToken).ConfigureAwait(false);

        return new PythagorasData
        {
            Estates = estates,
            Buildings = buildings,
            Floors = floors,
            Workspaces = workspaces
        };
    }

    private async Task<IReadOnlyList<Floor>> FetchFloorsViaWorkspaceBatchesAsync(
        IReadOnlyList<Workspace> workspaces,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Deriving floor identifiers from workspace payload for batch floor retrieval");

        const int floorBatchSize = 100;

        HashSet<int> floorIdsFromWorkspaces = [];
        HashSet<int> buildingsWithWorkspaceFloors = [];

        foreach (Workspace workspace in workspaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (workspace.FloorId is int floorId && floorId > 0)
            {
                floorIdsFromWorkspaces.Add(floorId);

                if (workspace.BuildingId is int buildingId && buildingId > 0)
                {
                    buildingsWithWorkspaceFloors.Add(buildingId);
                }
            }
        }

        Dictionary<int, Floor> floorsById = [];
        object syncRoot = new();

        if (floorIdsFromWorkspaces.Count > 0)
        {
            int batchCount = (floorIdsFromWorkspaces.Count + floorBatchSize - 1) / floorBatchSize;
            logger.LogDebug("Fetching floors in {BatchCount} batch(es) derived from {FloorIdCount} unique workspace floor ids", batchCount, floorIdsFromWorkspaces.Count);

            foreach (int[] batch in floorIdsFromWorkspaces.Chunk(floorBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                PythagorasQuery<Floor> query = new PythagorasQuery<Floor>()
                    .WithQueryParameterValues("floorIds[]", batch);

                IReadOnlyList<Floor> batchFloors = await pythagorasClient.GetFloorsAsync(query, cancellationToken).ConfigureAwait(false);

                lock (syncRoot)
                {
                    foreach (Floor floor in batchFloors)
                    {
                        floorsById[floor.Id] = floor;

                        if (floor.BuildingId is int buildingId && buildingId > 0)
                        {
                            buildingsWithWorkspaceFloors.Add(buildingId);
                        }
                    }
                }
            }
        }
        else
        {
            logger.LogWarning("No floor ids could be derived from workspace data; falling back to per-building floor fetch.");
        }

        // Fallback floor fetching is currently disabled as workspace-derived approach covers all floors
        // If needed in the future, uncomment the call below
        // const int fallbackConcurrency = 5;
        // await FetchFloorFallbackAsync(buildings, buildingsWithWorkspaceFloors, floorsById, syncRoot, fallbackConcurrency, cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Floor fetch completed: {TotalFloors} total (workspace-derived, fallback: disabled)",
            floorsById.Count);

        return [.. floorsById.Values];
    }

    /// <summary>
    /// Fallback method to fetch floors for buildings that don't have workspace-derived floors.
    /// Currently disabled as workspace-derived approach appears to cover all floors.
    /// </summary>
#pragma warning disable IDE0051 // Remove unused private members
    private async Task FetchFloorFallbackAsync(
#pragma warning restore IDE0051 // Remove unused private members
        IReadOnlyList<BuildingInfo> buildings,
        HashSet<int> buildingsWithWorkspaceFloors,
        Dictionary<int, Floor> floorsById,
        object syncRoot,
        int fallbackConcurrency,
        CancellationToken cancellationToken)
    {
        List<int> fallbackBuildingIds = [];
        foreach (BuildingInfo building in buildings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (building.Id > 0 && !buildingsWithWorkspaceFloors.Contains(building.Id))
            {
                fallbackBuildingIds.Add(building.Id);
            }
        }

        if (fallbackBuildingIds.Count == 0)
        {
            logger.LogDebug("No buildings require fallback floor fetching");
            return;
        }

        logger.LogDebug("Fetching floors for {BuildingCount} building(s) lacking workspace-derived floors using fallback endpoint", fallbackBuildingIds.Count);

        SemaphoreSlim semaphore = new(fallbackConcurrency);
        List<Task> tasks = new(fallbackBuildingIds.Count);

        foreach (int buildingId in fallbackBuildingIds)
        {
            tasks.Add(FetchFloorsForBuildingAsync(buildingId));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        async Task FetchFloorsForBuildingAsync(int buildingId)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IReadOnlyList<Floor> buildingFloors = await pythagorasClient.GetBuildingFloorsAsync(buildingId, query: null, cancellationToken).ConfigureAwait(false);

                lock (syncRoot)
                {
                    foreach (Floor floor in buildingFloors)
                    {
                        floorsById[floor.Id] = floor;

                        if (floor.BuildingId is int floorBuildingId && floorBuildingId > 0)
                        {
                            buildingsWithWorkspaceFloors.Add(floorBuildingId);
                        }
                    }
                }

                logger.LogDebug("Fetched {FloorCount} floors for building {BuildingId} via fallback", buildingFloors.Count, buildingId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to fetch floors for building {BuildingId} during fallback retrieval", buildingId);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
