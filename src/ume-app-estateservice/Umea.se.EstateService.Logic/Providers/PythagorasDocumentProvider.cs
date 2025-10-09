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
        IReadOnlyDictionary<int, AddressModel?> buildingAddresses = await LoadBuildingAddressesAsync().ConfigureAwait(false);
        await AddEstatesAndBuildings(docs, buildingAddresses).ConfigureAwait(false);
        await AddWorkspaces(docs, buildingAddresses).ConfigureAwait(false);
        return [.. docs.Values];
    }

    private async Task<IReadOnlyDictionary<int, AddressModel?>> LoadBuildingAddressesAsync()
    {
        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasHandler.GetBuildingsAsync().ConfigureAwait(false);
        if (buildings.Count == 0)
        {
            return new Dictionary<int, AddressModel?>();
        }

        Dictionary<int, AddressModel?> result = new(buildings.Count);
        foreach (BuildingInfoModel building in buildings)
        {
            result[building.Id] = building.Address;
        }

        return result;
    }

    private async Task AddEstatesAndBuildings(
        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs,
        IReadOnlyDictionary<int, AddressModel?> buildingAddresses)
    {
        PythagorasQuery<NavigationFolder> query = new PythagorasQuery<NavigationFolder>()
            .WithQueryParameter("includeAscendantBuildings", true);

        IReadOnlyList<EstateModel> estates = await pythagorasHandler.GetEstatesAsync(query).ConfigureAwait(false);

        foreach (EstateModel estate in estates)
        {
            PythagorasDocument estateDoc = CreateDocumentFromSearchable(estate);
            docs[estateDoc.Key] = estateDoc;

            foreach (BuildingModel building in estate.Buildings ?? [])
            {
                if (buildingAddresses.TryGetValue(building.Id, out AddressModel? address))
                {
                    building.Address = address;
                }

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

    private async Task AddWorkspaces(
        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs,
        IReadOnlyDictionary<int, AddressModel?> buildingAddresses)
    {
        PythagorasQuery<Workspace> query = new();

        IReadOnlyList<RoomModel> workspaces = await pythagorasHandler.GetRoomsAsync(query).ConfigureAwait(false);

        foreach (RoomModel workspace in workspaces)
        {
            PythagorasDocument doc = CreateDocumentFromSearchable(workspace);

            docs[doc.Key] = doc;

            if (workspace.BuildingId is int buildingId)
            {
                PythagorasDocument.DocumentKey buildingKey = new(NodeType.Building, buildingId);
                if (buildingAddresses.TryGetValue(buildingId, out AddressModel? address))
                {
                    doc.Address = FormatAddress(address);
                }

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
            Address = FormatAddress(item.Address),
            PopularName = item.PopularName,
            Geo = MapGeoLocation(item.GeoLocation),
            RankScore = rankScore,
            UpdatedAt = item.UpdatedAt,
            Ancestors = []
        };
    }

    private static string? FormatAddress(AddressModel? address)
    {
        if (address is null)
        {
            return null;
        }

        string[] parts =
        [
            address.Street,
            address.ZipCode,
            address.City
        ];

        string formatted = string.Join(' ', parts.Where(static p => !string.IsNullOrWhiteSpace(p)));
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
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
