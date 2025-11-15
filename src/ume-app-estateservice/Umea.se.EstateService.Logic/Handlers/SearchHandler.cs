using Microsoft.Extensions.Options;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Options;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public class SearchHandler(
    IPythagorasDocumentProvider documentProvider,
    IOptions<SearchOptions> searchOptions)
    : IIndexedPythagorasDocumentReader
{
    private readonly IPythagorasDocumentProvider _documentProvider = documentProvider;
    private readonly bool _excludeRooms = searchOptions.Value.ExcludeRooms;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private InMemorySearchService? _searchService;
    private IReadOnlyList<PythagorasDocument>? _indexedDocuments;

    public int GetDocumentCount() => _searchService?.DocumentCount ?? 0;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string? query,
        IReadOnlyCollection<AutocompleteType> types,
        int limit,
        GeoFilter? geoFilter = null,
        CancellationToken cancellationToken = default)
    {
        bool hasGeo = geoFilter is not null;
        if (string.IsNullOrWhiteSpace(query) && !hasGeo)
        {
            return [];
        }

        InMemorySearchService service = await EnsureSearchServiceAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyCollection<NodeType>? filterByTypes = BuildNodeTypeFilter(types);

        QueryOptions options = new(
            MaxResults: Math.Max(limit, 1),
            FilterByTypes: filterByTypes,
            GeoFilter: geoFilter);

        IEnumerable<SearchResult> results = service.Search(query ?? string.Empty, options);

        return [.. results.Take(limit)];
    }

    private async Task<InMemorySearchService> EnsureSearchServiceAsync(CancellationToken cancellationToken)
    {
        if (_searchService is { } existing)
        {
            return existing;
        }

        await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _searchService ?? await BuildSearchServiceAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    public async Task RefreshIndexAsync(CancellationToken cancellationToken = default)
    {
        await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await BuildSearchServiceAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private async Task<InMemorySearchService> BuildSearchServiceAsync(CancellationToken _)
    {
        ICollection<PythagorasDocument> documents = await _documentProvider.GetDocumentsAsync().ConfigureAwait(false);
        List<PythagorasDocument> snapshot = [.. documents];
        IEnumerable<PythagorasDocument> documentsToIndex = _excludeRooms
            ? snapshot.Where(static doc => doc.Type != NodeType.Room)
            : snapshot;

        InMemorySearchService service = new(documentsToIndex);
        _searchService = service;
        _indexedDocuments = snapshot;
        return service;
    }

    private static HashSet<NodeType>? BuildNodeTypeFilter(IReadOnlyCollection<AutocompleteType> types)
    {
        if (types.Count == 0 || types.Contains(AutocompleteType.Any))
        {
            return null;
        }

        HashSet<NodeType> nodeTypes = [];
        foreach (AutocompleteType type in types)
        {
            if (_autoCompleteTypeToNodeType.TryGetValue(type, out NodeType nodeType))
            {
                nodeTypes.Add(nodeType);
            }
        }

        return nodeTypes.Count == 0 ? null : nodeTypes;
    }

    private static readonly Dictionary<AutocompleteType, NodeType> _autoCompleteTypeToNodeType = new()
    {
        { AutocompleteType.Building, NodeType.Building },
        { AutocompleteType.Room, NodeType.Room },
        { AutocompleteType.Estate, NodeType.Estate }
    };

    public async Task<IReadOnlyCollection<PythagorasDocument>> GetIndexedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSearchServiceAsync(cancellationToken).ConfigureAwait(false);
        return _indexedDocuments ?? [];
    }

    public async Task<IReadOnlyDictionary<int, PythagorasDocument>> GetBuildingDocumentsByIdsAsync(IEnumerable<int> buildingIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildingIds);

        HashSet<int> idSet = [.. buildingIds.Where(static id => id > 0)];

        if (idSet.Count == 0)
        {
            return new Dictionary<int, PythagorasDocument>();
        }

        IReadOnlyCollection<PythagorasDocument> documents = await GetIndexedDocumentsAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<int, PythagorasDocument> result = new(idSet.Count);
        foreach (PythagorasDocument document in documents)
        {
            if (document.Type == NodeType.Building && idSet.Contains(document.Id))
            {
                result[document.Id] = document;
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<PythagorasDocument>> GetBuildingsForEstateAsync(int estateId, CancellationToken cancellationToken = default)
    {
        if (estateId <= 0)
        {
            return [];
        }

        IReadOnlyCollection<PythagorasDocument> documents = await GetIndexedDocumentsAsync(cancellationToken).ConfigureAwait(false);
        List<PythagorasDocument> result = [];

        foreach (PythagorasDocument document in documents)
        {
            if (document.Type != NodeType.Building)
            {
                continue;
            }

            if (document.Ancestors is { Count: > 0 } ancestors &&
                ancestors.Any(ancestor => ancestor.Type == NodeType.Estate && ancestor.Id == estateId))
            {
                result.Add(document);
            }
        }

        return result;
    }
}
