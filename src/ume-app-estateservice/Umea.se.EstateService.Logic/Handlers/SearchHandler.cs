using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Handlers;

public class SearchHandler(IPythagorasHandler pythagorasHandler)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ICollection<PythagorasDocument>> GetPythagorasDocumentsAsync()
    {
        Dictionary<string, PythagorasDocument> docs = [];

        await AddEstatesAndBuildings(docs);
        await AddWorkspaces(docs);

        return [.. docs.Values];
    }

    private async Task AddEstatesAndBuildings(Dictionary<string, PythagorasDocument> docs)
    {
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .WithQueryParameter("includeAscendantBuildings", true);

        IReadOnlyList<EstateModel> estates = await pythagorasHandler.GetEstatesAsync(query);

        foreach (EstateModel estate in estates)
        {
            PythagorasDocument estateDoc = CreateDocumentFromSearchable(estate);
            docs[estateDoc.Id] = estateDoc;

            foreach (BuildingModel building in estate.Buildings ?? [])
            {
                string key = $"building-{building.Id}";
                if (!docs.TryGetValue(key, out PythagorasDocument? doc))
                {
                    doc = CreateDocumentFromSearchable(building);
                    docs[doc.Id] = doc;
                }

                doc.Ancestors.Add(CreateAncestorFromDocument(estateDoc));
            }
        }
    }

    private async Task AddWorkspaces(Dictionary<string, PythagorasDocument> docs)
    {
        PythagorasQuery<Workspace> query = new();

        IReadOnlyList<WorkspaceModel> workspaces = await pythagorasHandler.GetWorkspacesAsync(query);

        foreach (WorkspaceModel workspace in workspaces)
        {
            PythagorasDocument doc = CreateDocumentFromSearchable(workspace);

            docs[doc.Id] = doc;

            if(workspace.BuildingId is int buildingId)
{
                string buildingKey = $"building-{buildingId}";
                if (docs.TryGetValue(buildingKey, out PythagorasDocument? buildingDoc))
                {
                    doc.Ancestors.AddRange(buildingDoc.Ancestors);
                    doc.Ancestors.Add(CreateAncestorFromDocument(buildingDoc));
                }
            }
        }
    }

    private static PythagorasDocument CreateDocumentFromSearchable(ISearchable item)
    {
        (NodeType nodeType, int rankScore) = item switch
        {
            EstateModel => (NodeType.Estate, 1),
            BuildingModel => (NodeType.Building, 2),
            WorkspaceModel => (NodeType.Room, 3),
            _ => throw new ArgumentException($"Unknown searchable type: {item.GetType().Name}", nameof(item))
        };

        string idPrefix = nodeType.ToString().ToLower();

        return new PythagorasDocument
        {
            Id = $"{idPrefix}-{item.Id}",
            Type = nodeType,
            Name = item.Name,
            Address = item.Address != null ? $"{item.Address.Street} {item.Address.ZipCode} {item.Address.City}" : null,
            PopularName = item.PopularName,
            Geo = MapGeoLocation(item.GeoLocation),
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

#pragma warning disable IDE0051 // Remove unused private members
    private string SerializeObject<T>(T obj) => JsonSerializer.Serialize(obj, _jsonOptions);
#pragma warning restore IDE0051 // Remove unused private members
}
