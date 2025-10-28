using System.Linq;
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
{
    private readonly IPythagorasDocumentProvider _documentProvider = documentProvider;
    private readonly bool _excludeRooms = searchOptions.Value.ExcludeRooms;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private InMemorySearchService? _searchService;

    public Task<ICollection<PythagorasDocument>> GetPythagorasDocumentsAsync()
        => _documentProvider.GetDocumentsAsync();

    public int GetDocumentCount() => _searchService?.DocumentCount ?? 0;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, IReadOnlyCollection<AutocompleteType> types, int limit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        InMemorySearchService service = await EnsureSearchServiceAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyCollection<NodeType>? filterByTypes = BuildNodeTypeFilter(types);

        QueryOptions options = new(MaxResults: Math.Max(limit, 1), FilterByTypes: filterByTypes);
        IEnumerable<SearchResult> results = service.Search(query, options);

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
        IEnumerable<PythagorasDocument> documentsToIndex = _excludeRooms
            ? documents.Where(static doc => doc.Type != NodeType.Room)
            : documents;

        InMemorySearchService service = new(documentsToIndex);
        _searchService = service;
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
}
