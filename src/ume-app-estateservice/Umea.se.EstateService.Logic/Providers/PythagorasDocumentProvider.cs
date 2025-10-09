using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Interfaces;
using Umea.se.EstateService.Shared.Models;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Providers;

public class PythagorasDocumentProvider(IPythagorasHandler pythagorasHandler) : IPythagorasDocumentProvider
{
    public async Task<ICollection<PythagorasDocument>> GetDocumentsAsync()
    {
        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs = [];
        await AddEstatesAndBuildings(docs).ConfigureAwait(false);
        await AddWorkspaces(docs).ConfigureAwait(false);
        return [.. docs.Values];
    }

    private async Task AddEstatesAndBuildings(Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs)
    {
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .WithQueryParameter("includeAscendantBuildings", true);

        IReadOnlyList<EstateModel> estates = await pythagorasHandler.GetEstatesAsync(query);

        foreach (EstateModel estate in estates)
        {
            PythagorasDocument estateDoc = CreateDocumentFromSearchable(estate);
            docs[estateDoc.Key] = estateDoc;

            foreach (BuildingModel building in estate.Buildings ?? [])
            {
                PythagorasDocument.DocumentKey buildingKey = new(NodeType.Building, building.Id);
                if (!docs.TryGetValue(buildingKey, out PythagorasDocument? doc))
                {
                    doc = CreateDocumentFromSearchable(building);
                    docs[doc.Key] = doc;
                }

                doc.Ancestors.Add(CreateAncestorFromDocument(estateDoc));
            }
        }
    }

    private async Task AddWorkspaces(Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs)
    {
        PythagorasQuery<Workspace> query = new();

        IReadOnlyList<RoomModel> workspaces = await pythagorasHandler.GetRoomsAsync(query);

        foreach (RoomModel workspace in workspaces)
        {
            PythagorasDocument doc = CreateDocumentFromSearchable(workspace);

            docs[doc.Key] = doc;

            if (workspace.BuildingId is int buildingId)
            {
                PythagorasDocument.DocumentKey buildingKey = new(NodeType.Building, buildingId);
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
            RoomModel => (NodeType.Room, 3),
            _ => throw new ArgumentException($"Unknown searchable type: {item.GetType().Name}", nameof(item))
        };

        return new PythagorasDocument
        {
            Id = item.Id,
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
}
