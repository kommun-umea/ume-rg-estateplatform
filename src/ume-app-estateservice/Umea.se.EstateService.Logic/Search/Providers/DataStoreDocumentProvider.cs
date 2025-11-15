using Umea.se.EstateService.Logic.Data.Entities;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.ServiceAccess.Data;
using Umea.se.EstateService.Shared.Search;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Logic.Search.Providers;

/// <summary>
/// Provides Pythagoras documents by reading from the in-memory data store.
/// This implementation uses the cached entity data instead of making API calls.
/// </summary>
public class DataStoreDocumentProvider(IDataStore dataStore) : IPythagorasDocumentProvider
{
    public Task<ICollection<PythagorasDocument>> GetDocumentsAsync()
    {
        if (!dataStore.IsReady)
        {
            throw new InvalidOperationException("Data store is not ready. Cannot generate search documents.");
        }

        Dictionary<PythagorasDocument.DocumentKey, PythagorasDocument> docs = [];

        // Process all estates and their hierarchies
        foreach (EstateEntity estate in dataStore.Estates)
        {
            PythagorasDocument estateDoc = CreateDocumentFromEstate(estate);
            docs[estateDoc.Key] = estateDoc;

            // Add buildings under this estate
            foreach (BuildingEntity building in estate.Buildings)
            {
                PythagorasDocument buildingDoc = CreateDocumentFromBuilding(building);
                docs[buildingDoc.Key] = buildingDoc;
                LinkParent(buildingDoc, estateDoc);

                // Add rooms under this building
                foreach (RoomEntity room in building.Rooms)
                {
                    PythagorasDocument roomDoc = CreateDocumentFromRoom(room, building);
                    docs[roomDoc.Key] = roomDoc;
                    LinkParent(roomDoc, buildingDoc);
                }
            }
        }

        return Task.FromResult<ICollection<PythagorasDocument>>([.. docs.Values]);
    }

    private static PythagorasDocument CreateDocumentFromEstate(EstateEntity estate)
    {
        return new PythagorasDocument
        {
            Id = estate.Id,
            Type = NodeType.Estate,
            Name = estate.Name,
            PopularName = estate.PopularName,
            Address = estate.Address,
            GeoLocation = MapGeoLocation(estate.GeoLocation),
            GrossArea = estate.GrossArea,
            RankScore = 1,
            UpdatedAt = estate.UpdatedAt,
            Ancestors = [],
            NumChildren = estate.Buildings.Count,
            ExtendedProperties = CreateEstateExtendedProperties(estate)
        };
    }

    private static PythagorasDocument CreateDocumentFromBuilding(BuildingEntity building)
    {
        return new PythagorasDocument
        {
            Id = building.Id,
            Type = NodeType.Building,
            Name = building.Name,
            PopularName = building.PopularName,
            Address = building.Address,
            GeoLocation = MapGeoLocation(building.GeoLocation),
            GrossArea = building.GrossArea,
            RankScore = 2,
            UpdatedAt = building.UpdatedAt,
            Ancestors = [],
            NumFloors = building.Floors.Count,
            NumRooms = building.Rooms.Count,
            ExtendedProperties = CreateBuildingExtendedProperties(building)
        };
    }

    private static PythagorasDocument CreateDocumentFromRoom(RoomEntity room, BuildingEntity building)
    {
        return new PythagorasDocument
        {
            Id = room.Id,
            Type = NodeType.Room,
            Name = room.Name,
            PopularName = room.PopularName,
            // Use building address/geo so room documents have direct location context,
            // while ancestry still links them to the building/estate hierarchy.
            Address = building.Address,
            GeoLocation = MapGeoLocation(building.GeoLocation),
            GrossArea = (decimal?)room.GrossArea,
            RankScore = 3,
            UpdatedAt = room.UpdatedAt,
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

    private static GeoPoint? MapGeoLocation(GeoPointModel? geoLocationModel)
    {
        return geoLocationModel != null
            ? new GeoPoint { Lat = geoLocationModel.Lat, Lng = geoLocationModel.Lon }
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

    private static void LinkParent(PythagorasDocument child, PythagorasDocument parent)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(parent);

        parent.NumChildren++;

        child.Ancestors.Clear();
        child.Ancestors.AddRange(parent.Ancestors);
        child.Ancestors.Add(CreateAncestorFromDocument(parent));
    }

    private static Dictionary<string, string>? CreateEstateExtendedProperties(EstateEntity estate)
    {
        return MapExtendedProperties(
            ("operationalArea", estate.OperationalArea),
            ("municipalityArea", estate.MunicipalityArea),
            ("propertyDesignation", estate.PropertyDesignation));
    }

    private static Dictionary<string, string>? CreateBuildingExtendedProperties(BuildingEntity building)
    {
        return MapExtendedProperties(
            ("yearOfConstruction", building.YearOfConstruction),
            ("externalOwner", building.ExternalOwner),
            ("propertyDesignation", building.PropertyDesignation));
    }

    private static Dictionary<string, string>? MapExtendedProperties(params (string Key, string? Value)[] properties)
    {
        Dictionary<string, string> result = properties
            .Where(static p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(static p => p.Key, static p => p.Value!);

        return result.Count > 0 ? result : null;
    }
}
