using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public class SearchHandler(IPythagorasDocumentProvider documentProvider)
{
    private readonly IPythagorasDocumentProvider _documentProvider = documentProvider;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private InMemorySearchService? _searchService;

    public Task<ICollection<PythagorasDocument>> GetPythagorasDocumentsAsync()
        => _documentProvider.GetDocumentsAsync();

    public int GetDocumentCount() => _searchService?.DocumentCount ?? 0;

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, AutocompleteType type, int limit, int? buildingId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        InMemorySearchService service = await EnsureSearchServiceAsync(cancellationToken).ConfigureAwait(false);

        // Pass type filter to search so it can return the right number of results
        NodeType? filterByType = null;
        if (type != AutocompleteType.Any && _autoCompleteTypeToNodeType.TryGetValue(type, out NodeType nodeType))
        {
            filterByType = nodeType;
        }

        QueryOptions options = new(MaxResults: Math.Max(limit, 1), FilterByType: filterByType);
        IEnumerable<SearchResult> results = service.Search(query, options);

        results = ApplyBuildingFilter(results, type, buildingId);

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
        InMemorySearchService service = new(documents);
        _searchService = service;
        return service;
    }

    private static IEnumerable<SearchResult> ApplyBuildingFilter(IEnumerable<SearchResult> results, AutocompleteType type, int? buildingId)
    {
        if (buildingId is null)
        {
            return results;
        }

        int buildingIdValue = buildingId.Value;

        return type switch
        {
            AutocompleteType.Building => results.Where(r => r.Item.Type == NodeType.Building && r.Item.Id == buildingIdValue),
            _ => results.Where(r => (r.Item.Type == NodeType.Building && r.Item.Id == buildingIdValue) ||
                                     r.Item.Ancestors.Any(a => a.Type == NodeType.Building && a.Id == buildingIdValue))
        };
    }

    private static readonly Dictionary<AutocompleteType, NodeType> _autoCompleteTypeToNodeType = new()
    {
        { AutocompleteType.Building, NodeType.Building },
        { AutocompleteType.Room, NodeType.Room }
    };
}
