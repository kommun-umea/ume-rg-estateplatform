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
        IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos = await LoadBuildingInfosAsync().ConfigureAwait(false);
        await AddEstatesAndBuildings(docs, buildingInfos).ConfigureAwait(false);
        await AddWorkspaces(docs, buildingInfos).ConfigureAwait(false);
        return [.. docs.Values];
    }

    private async Task<IReadOnlyDictionary<int, BuildingInfoModel>> LoadBuildingInfosAsync()
    {
        IReadOnlyList<BuildingInfoModel> buildings = await pythagorasHandler
            .GetBuildingsWithPropertiesAsync(cancellationToken: default)
            .ConfigureAwait(false);
        if (buildings.Count == 0)
        {
            return new Dictionary<int, BuildingInfoModel>();
        }

        Dictionary<int, BuildingInfoModel> result = new(buildings.Count);
        foreach (BuildingInfoModel building in buildings)
        {
            result[building.Id] = building;
        }

        return result;
    }

    private async Task AddEstatesAndBuildings(
        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs,
        IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos)
    {
        IReadOnlyList<EstateModel> estates = await pythagorasHandler
            .GetEstatesWithPropertiesAsync(includeBuildings: true)
            .ConfigureAwait(false);

        foreach (EstateModel estate in estates)
        {
            PythagorasDocument estateDoc = CreateDocumentFromSearchable(estate);
            estateDoc.GrossArea = estate.GrossArea;
            estateDoc.ExtendedProperties = CreateEstateExtendedProperties(estate.ExtendedProperties);
            docs[estateDoc.Key] = estateDoc;

            foreach (BuildingModel building in estate.Buildings ?? [])
            {
                estateDoc.NumChildren++;

                if (buildingInfos.TryGetValue(building.Id, out BuildingInfoModel? buildingInfo))
                {
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
                    doc.GrossArea = buildingInfo.GrossArea;
                    doc.ExtendedProperties = CreateBuildingExtendedProperties(buildingInfo.ExtendedProperties);
                }

                doc.Ancestors.Add(CreateAncestorFromDocument(estateDoc));
            }
        }
    }

    private async Task AddWorkspaces(
        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs,
        IReadOnlyDictionary<int, BuildingInfoModel> buildingInfos)
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
                if (buildingInfos.TryGetValue(buildingId, out BuildingInfoModel? buildingInfo))
                {
                    doc.Address = FormatAddress(buildingInfo.Address);
                }

                if (docs.TryGetValue(buildingKey, out PythagorasDocument? buildingDoc))
                {
                    buildingDoc.NumChildren++;
                    doc.Ancestors.AddRange(buildingDoc.Ancestors);
                    doc.Ancestors.Add(CreateAncestorFromDocument(buildingDoc));
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

    private static IReadOnlyDictionary<string, string>? CreateBuildingExtendedProperties(BuildingExtendedPropertiesModel? source)
    {
        if (source is null)
        {
            return null;
        }

        Dictionary<string, string> result = new(2);

        if (!string.IsNullOrWhiteSpace(source.YearOfConstruction))
        {
            result["yearOfConstruction"] = source.YearOfConstruction;
        }

        if (!string.IsNullOrWhiteSpace(source.ExternalOwner))
        {
            result["externalOwner"] = source.ExternalOwner;
        }

        return result.Count == 0 ? null : result;
    }

    private static IReadOnlyDictionary<string, string>? CreateEstateExtendedProperties(EstateExtendedPropertiesModel? source)
    {
        if (source is null)
        {
            return null;
        }

        Dictionary<string, string> result = new(3);

        if (!string.IsNullOrWhiteSpace(source.OperationalArea))
        {
            result["operationalArea"] = source.OperationalArea;
        }

        if (!string.IsNullOrWhiteSpace(source.MunicipalityArea))
        {
            result["municipalityArea"] = source.MunicipalityArea;
        }

        if (!string.IsNullOrWhiteSpace(source.PropertyDesignation))
        {
            result["propertyDesignation"] = source.PropertyDesignation;
        }

        return result.Count == 0 ? null : result;
    }
}
