using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Search.Providers;

public class PythagorasDocumentProvider(IPythagorasHandler pythagorasHandler) : IPythagorasDocumentProvider
{
    public async Task<ICollection<PythagorasDocument>> GetDocumentsAsync()
    {
        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs = [];
        IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos = await LoadBuildingInfosAsync().ConfigureAwait(false);
        IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> workspaceStats = await LoadBuildingWorkspaceStatsAsync().ConfigureAwait(false);
        await AddEstatesAndBuildings(docs, buildingInfos, workspaceStats).ConfigureAwait(false);
        await AddWorkspaces(docs, buildingInfos, workspaceStats).ConfigureAwait(false);
        return [.. docs.Values];
    }

    private async Task<IReadOnlyDictionary<int, BuildingInfoModel>> LoadBuildingInfosAsync()
    {
        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasHandler
            .GetBuildingsAsync(buildingIds: null, estateId: null, includeOptions: ServiceAccess.Pythagoras.Enum.BuildingIncludeOptions.ExtendedProperties, queryArgs: null, cancellationToken: default)
            .ConfigureAwait(false);
        if (buildings.Count == 0)
        {
            return new Dictionary<int, BuildingInfoModel>();
        }

        return buildings.ToDictionary(static building => building.Id);
    }

    private async Task<IReadOnlyDictionary<int, BuildingWorkspaceStatsModel>> LoadBuildingWorkspaceStatsAsync()
    {
        IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> stats = await pythagorasHandler
            .GetBuildingWorkspaceStatsAsync(cancellationToken: default)
            .ConfigureAwait(false);

        return stats.Count == 0 ? new Dictionary<int, BuildingWorkspaceStatsModel>() : stats;
    }

    private async Task AddEstatesAndBuildings(Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs, IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos, IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> workspaceStats)
    {
        // This method is a bit complicated due to Pythagoras API doesn't have an endpoint that both can
        // return estates with buildings and building extended properties in one call.
        ArgumentNullException.ThrowIfNull(workspaceStats);

        Task<IReadOnlyList<EstateModel>> estatesWithBuildingsTask = pythagorasHandler.GetEstatesWithBuildingsAsync();
        Task<IReadOnlyList<EstateModel>> estatesWithPropertiesTask = pythagorasHandler.GetEstatesWithPropertiesAsync();

        await Task.WhenAll(estatesWithBuildingsTask, estatesWithPropertiesTask).ConfigureAwait(false);

        IReadOnlyList<EstateModel> estatesWithBuildings = estatesWithBuildingsTask.Result;

        if (estatesWithBuildings.Count == 0)
        {
            return;
        }

        IReadOnlyList<EstateModel> estatesWithProperties = estatesWithPropertiesTask.Result;

        Dictionary<int, EstateExtendedPropertiesModel?> estateExtendedProperties = estatesWithProperties
            .ToDictionary(static estate => estate.Id, static estate => estate.ExtendedProperties);

        foreach (EstateModel estate in estatesWithBuildings)
        {
            PythagorasDocument estateDoc = CreateDocumentFromSearchable(estate);
            estateDoc.GrossArea = estate.GrossArea;
            estateDoc.ExtendedProperties = CreateEstateExtendedProperties(
                estateExtendedProperties.TryGetValue(estate.Id, out EstateExtendedPropertiesModel? extendedProperties)
                    ? extendedProperties
                    : null);
            docs[estateDoc.Key] = estateDoc;

            foreach (BuildingModel building in estate.Buildings ?? [])
            {
                PythagorasDocument buildingDoc = GetOrAddBuildingDocument(docs, building, buildingInfos, workspaceStats);
                LinkParent(buildingDoc, estateDoc);
            }
        }
    }

    private async Task AddWorkspaces(Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs, IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos, IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> workspaceStats)
    {
        ArgumentNullException.ThrowIfNull(workspaceStats);

        IReadOnlyList<RoomModel> workspaces = await pythagorasHandler.GetRoomsAsync().ConfigureAwait(false);

        foreach (RoomModel workspace in workspaces)
        {
            PythagorasDocument doc = CreateDocumentFromSearchable(workspace);

            docs[doc.Key] = doc;

            if (workspace.BuildingId is int buildingId)
            {
                PythagorasDocument.DocumentKey buildingKey = new(NodeType.Building, buildingId);
                if (buildingInfos.TryGetValue(buildingId, out BuildingInfoModel? buildingInfo))
                {
                    doc.Address = buildingInfo.Address;
                }

                if (docs.TryGetValue(buildingKey, out PythagorasDocument? buildingDoc))
                {
                    ApplyWorkspaceStats(buildingDoc, workspaceStats);
                    LinkParent(doc, buildingDoc);
                }
            }

            doc.GrossArea = (decimal?)workspace.GrossArea;
        }
    }
    private static PythagorasDocument CreateDocumentFromSearchable(ISearchable item)
    {
        (NodeType nodeType, int rankScore) = item switch
        {
            EstateModel => (NodeType.Estate, 1),
            BuildingModel => (NodeType.Building, 2),
            RoomModel => (NodeType.Room, 3),
            _ => throw new ArgumentException($"Unknown searchable type: {item.GetType().Name}", nameof(item))
        };

        return new PythagorasDocument
        {
            Id = item.Id,
            Type = nodeType,
            Name = item.Name,
            Address = item.Address,
            PopularName = item.PopularName,
            GeoLocation = MapGeoLocation(item.GeoLocation),
            RankScore = rankScore,
            UpdatedAt = item.UpdatedAt,
            Ancestors = []
        };
    }

    private static Shared.Search.GeoPoint? MapGeoLocation(GeoPointModel? geoLocationModel)
    {
        return geoLocationModel != null
            ? new Shared.Search.GeoPoint { Lat = geoLocationModel.Lat, Lng = geoLocationModel.Lon }
            : null;
    }

    private static Ancestor CreateAncestorFromDocument(PythagorasDocument doc)
    {
        return new Ancestor
        {
            Id = doc.Id,
            Type = doc.Type,
            Name = doc.Name,
            PopularName = doc.PopularName
        };
    }

    private static Dictionary<string, string>? CreateBuildingExtendedProperties(BuildingExtendedPropertiesModel? source)
    {
        if (source is null)
        {
            return null;
        }

        return MapExtendedProperties(
            ("yearOfConstruction", source.YearOfConstruction),
            ("externalOwner", source.ExternalOwner));
    }

    private static Dictionary<string, string>? CreateEstateExtendedProperties(EstateExtendedPropertiesModel? source)
    {
        if (source is null)
        {
            return null;
        }

        return MapExtendedProperties(
            ("operationalArea", source.OperationalArea),
            ("municipalityArea", source.MunicipalityArea),
            ("propertyDesignation", source.PropertyDesignation));
    }

    private static void LinkParent(PythagorasDocument child, PythagorasDocument parent)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(parent);

        parent.NumChildren++;

        child.Ancestors.Clear();
        child.Ancestors.AddRange(parent.Ancestors);
        child.Ancestors.Add(CreateAncestorFromDocument(parent));
    }

    private static Dictionary<string, string>? MapExtendedProperties(params (string Key, string? Value)[] properties)
    {
        Dictionary<string, string> result = properties
            .Where(static p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(static p => p.Key, static p => p.Value!);

        return result.Count > 0 ? result : null;
    }

    private static void ApplyWorkspaceStats(PythagorasDocument doc, IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> workspaceStats)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(workspaceStats);

        if (workspaceStats.TryGetValue(doc.Id, out BuildingWorkspaceStatsModel? stats) && stats is not null)
        {
            doc.NumRooms = stats.NumberOfRooms;
            doc.NumFloors = stats.NumberOfFloors;
        }
        else
        {
            doc.NumRooms = null;
            doc.NumFloors = null;
        }
    }

    private PythagorasDocument GetOrAddBuildingDocument(Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs, BuildingModel building, IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos, IReadOnlyDictionary<int, BuildingWorkspaceStatsModel> workspaceStats)
    {
        ArgumentNullException.ThrowIfNull(docs);
        ArgumentNullException.ThrowIfNull(building);
        ArgumentNullException.ThrowIfNull(buildingInfos);
        ArgumentNullException.ThrowIfNull(workspaceStats);

        BuildingInfoModel? buildingInfo = null;
        if (buildingInfos.TryGetValue(building.Id, out BuildingInfoModel? info))
        {
            buildingInfo = info;
            building.Address = buildingInfo.Address;
        }

        PythagorasDocument.DocumentKey buildingKey = new(NodeType.Building, building.Id);
        if (!docs.TryGetValue(buildingKey, out PythagorasDocument? doc))
        {
            doc = CreateDocumentFromSearchable(building);
            docs[doc.Key] = doc;
        }

        if (buildingInfo is not null)
        {
            doc.Address = buildingInfo.Address;
            doc.GrossArea = buildingInfo.GrossArea;
            doc.ExtendedProperties = CreateBuildingExtendedProperties(buildingInfo.ExtendedProperties);
        }

        ApplyWorkspaceStats(doc, workspaceStats);

        return doc;
    }
}
