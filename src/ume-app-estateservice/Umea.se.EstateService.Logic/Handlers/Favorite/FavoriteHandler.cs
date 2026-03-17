using Umea.se.EstateService.Logic.Search.Providers;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers.Favorite;

public interface IFavoriteHandler
{
    Task SetFavoriteAsync(string email, NodeType nodeType, int nodeId, CancellationToken cancellationToken = default);
    Task RemoveFavoriteAsync(string email, NodeType nodeType, int nodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PythagorasDocument>> GetFavoritesAsync(string email, CancellationToken cancellationToken = default);
    Task<HashSet<(NodeType Type, int Id)>> GetFavoriteIdsAsync(string email, CancellationToken cancellationToken = default);
}

public class FavoriteHandler(IFavoriteRepository favoriteRepository, IDataStore dataStore) : IFavoriteHandler
{
    public async Task SetFavoriteAsync(string email, NodeType nodeType, int nodeId, CancellationToken cancellationToken = default)
    {
        FavoriteEntity favorite = new()
        {
            UserEmail = email,
            NodeType = nodeType,
            NodeId = nodeId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await favoriteRepository.AddAsync(favorite, cancellationToken);
    }

    public async Task RemoveFavoriteAsync(string email, NodeType nodeType, int nodeId, CancellationToken cancellationToken = default)
    {
        await favoriteRepository.RemoveAsync(email, nodeType, nodeId, cancellationToken);
    }

    public async Task<IReadOnlyList<PythagorasDocument>> GetFavoritesAsync(string email, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FavoriteEntity> favorites = await favoriteRepository.GetByEmailAsync(email, cancellationToken);

        List<PythagorasDocument> documents = [];

        foreach (FavoriteEntity fav in favorites)
        {
            PythagorasDocument? doc = CreateDocument(fav);
            if (doc is not null)
            {
                doc.IsFavorite = true;
                documents.Add(doc);
            }
        }

        return documents;
    }

    public async Task<HashSet<(NodeType Type, int Id)>> GetFavoriteIdsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await favoriteRepository.GetFavoriteIdsAsync(email, cancellationToken);
    }

    private PythagorasDocument? CreateDocument(FavoriteEntity favorite)
    {
        switch (favorite.NodeType)
        {
            case NodeType.Estate:
                if (!dataStore.EstatesById.TryGetValue(favorite.NodeId, out EstateEntity? estate))
                {
                    return null;
                }

                PythagorasDocument estateDoc = DataStoreDocumentProvider.CreateDocumentFromEstate(estate);
                return estateDoc;

            case NodeType.Building:
                if (!dataStore.BuildingsById.TryGetValue(favorite.NodeId, out BuildingEntity? building))
                {
                    return null;
                }

                PythagorasDocument buildingDoc = DataStoreDocumentProvider.CreateDocumentFromBuilding(building);
                buildingDoc.ImageUrl = EstateDataQueryHandler.GetBuildingImageUrl(building);

                // Link estate ancestor if available
                if (dataStore.EstatesById.TryGetValue(building.EstateId, out EstateEntity? parentEstate))
                {
                    PythagorasDocument parentEstateDoc = DataStoreDocumentProvider.CreateDocumentFromEstate(parentEstate);
                    DataStoreDocumentProvider.LinkParent(buildingDoc, parentEstateDoc);
                }

                return buildingDoc;

            case NodeType.Room:
                if (!dataStore.RoomsById.TryGetValue(favorite.NodeId, out RoomEntity? room))
                {
                    return null;
                }

                if (!dataStore.BuildingsById.TryGetValue(room.BuildingId, out BuildingEntity? roomBuilding))
                {
                    return null;
                }

                PythagorasDocument roomDoc = DataStoreDocumentProvider.CreateDocumentFromRoom(room, roomBuilding);

                // Build ancestor hierarchy: estate -> building -> room
                PythagorasDocument roomBuildingDoc = DataStoreDocumentProvider.CreateDocumentFromBuilding(roomBuilding);

                if (dataStore.EstatesById.TryGetValue(roomBuilding.EstateId, out EstateEntity? roomEstate))
                {
                    PythagorasDocument roomEstateDoc = DataStoreDocumentProvider.CreateDocumentFromEstate(roomEstate);
                    DataStoreDocumentProvider.LinkParent(roomBuildingDoc, roomEstateDoc);
                }

                DataStoreDocumentProvider.LinkParent(roomDoc, roomBuildingDoc);
                return roomDoc;

            default:
                return null;
        }
    }

}
