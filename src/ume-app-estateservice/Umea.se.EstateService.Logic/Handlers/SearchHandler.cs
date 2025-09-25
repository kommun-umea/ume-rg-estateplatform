using System.Text.Json;
using System.Text.Json.Serialization;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public class SearchHandler(IPythagorasHandler pythagorasHandler)
{
    private readonly JsonSerializerOptions _jsonOptions = new ()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<PythagorasDocument>> GetPythagorasDocuments()
    {
        IReadOnlyList<EstateModel> estates = await pythagorasHandler.GetEstatesAsync();
        //IReadOnlyList<BuildingInfoModel> buildings = await pythagorasHandler.GetBuildingInfoAsync();
        IReadOnlyList<BuildingModel> buildings = [];
        IReadOnlyList<WorkspaceModel> rooms = await pythagorasHandler.GetWorkspacesAsync();

        Dictionary<int, BuildingModel> buildingDict = buildings.ToDictionary(b => b.Id, b => b);

        List<PythagorasDocument> docs =
        [
            .. estates.Select(CreateDocumentFromEstate),
            .. buildings.Select(CreateDocumentFromBuilding),
            .. rooms.Select(room => CreateDocumentFromWorkspace(room, buildingDict)),
        ];

        return docs;
    }

    private static PythagorasDocument CreateDocumentFromEstate(EstateModel estate)
    {
        return new PythagorasDocument
        {
            Id = $"estate-{estate.Id}",
            Type = NodeType.Estate,
            Name = estate.Name,
            Address = estate.Address,
            Aliases = !string.IsNullOrEmpty(estate.PopularName) ? [estate.PopularName] : null,
            Geo = estate.GeoLocation != null ? new Shared.Search.GeoPoint { Lat = estate.GeoLocation.Lat, Lng = estate.GeoLocation.Lon } : null,
            UpdatedAt = DateTimeOffset.UtcNow,
            Ancestors = []
        };
    }

    private static PythagorasDocument CreateDocumentFromBuilding(BuildingModel building)
    {
        return new PythagorasDocument
        {
            Id = $"building-{building.Id}",
            Type = NodeType.Building,
            Name = building.Name,
            Aliases = !string.IsNullOrEmpty(building.PopularName) ? [building.PopularName] : null,
            Geo = building.GeoLocation != null ? new Shared.Search.GeoPoint { Lat = building.GeoLocation.Lat, Lng = building.GeoLocation.Lon } : null,
            UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(building.Updated),
            Ancestors = []
        };
    }

    private static PythagorasDocument CreateDocumentFromWorkspace(WorkspaceModel workspace, Dictionary<int, BuildingModel> buildingDict)
    {
        if (buildingDict.Count == -1)
        {
            return new PythagorasDocument();
        }

        return new PythagorasDocument
        {
            Id = $"room-{workspace.Id}",
            Type = NodeType.Room,
            Name = workspace.Name,
            Aliases = !string.IsNullOrEmpty(workspace.PopularName) ? [workspace.PopularName] : null,
            UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(workspace.Updated),
            Ancestors = []
        };
    }

#pragma warning disable IDE0051 // Remove unused private members
    private string SerializeObject<T>(T obj) => JsonSerializer.Serialize(obj, _jsonOptions);
#pragma warning restore IDE0051 // Remove unused private members
}
