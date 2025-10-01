using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Autocomplete;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Handlers;

public class SearchHandler(IPythagorasDocumentProvider pythagorasDocumentProvider)
{
    private readonly IPythagorasDocumentProvider _pythagorasDocumentProvider = pythagorasDocumentProvider;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private InMemorySearchService? _searchService;

    public Task<ICollection<PythagorasDocument>> GetPythagorasDocumentsAsync()
        => _pythagorasDocumentProvider.GetDocumentsAsync();

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        AutocompleteType type,
        int limit,
        int? buildingId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        InMemorySearchService service = await EnsureSearchServiceAsync(cancellationToken).ConfigureAwait(false);
        QueryOptions options = new QueryOptions(MaxResults: Math.Max(limit, 1));
        IEnumerable<SearchResult> results = service.Search(query, options);

        if (type != AutocompleteType.Any && AutoCompleteTypeToNodeType.TryGetValue(type, out NodeType nodeType))
        {
            results = results.Where(r => r.Item.Type == nodeType);
        }

        results = ApplyBuildingFilter(results, type, buildingId);

        return results.Take(limit).ToArray();
    }

    private async Task<InMemorySearchService> EnsureSearchServiceAsync(CancellationToken cancellationToken)
    {
        InMemorySearchService? existing = Volatile.Read(ref _searchService);
        if (existing != null)
        {
            return existing;
        }

        await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            existing = _searchService;
            if (existing != null)
            {
                return existing;
            }

            ICollection<PythagorasDocument> documents = await _pythagorasDocumentProvider.GetDocumentsAsync().ConfigureAwait(false);
            existing = new InMemorySearchService(documents);
            Volatile.Write(ref _searchService, existing);
            return existing;
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private static IEnumerable<SearchResult> ApplyBuildingFilter(IEnumerable<SearchResult> results, AutocompleteType type, int? buildingId)
    {
        if (buildingId is null)
        {
            return results;
        }

        string buildingKey = $"building-{buildingId.Value}";

        return type switch
        {
            AutocompleteType.Building => results.Where(r => string.Equals(r.Item.Id, buildingKey, StringComparison.OrdinalIgnoreCase)),
            _ => results.Where(r => string.Equals(r.Item.Id, buildingKey, StringComparison.OrdinalIgnoreCase) ||
                                     r.Item.Ancestors.Any(a => string.Equals(a.Id, buildingKey, StringComparison.OrdinalIgnoreCase)))
        };
    }

    private static readonly IReadOnlyDictionary<AutocompleteType, NodeType> AutoCompleteTypeToNodeType = new Dictionary<AutocompleteType, NodeType>
    {
        { AutocompleteType.Building, NodeType.Building },
        { AutocompleteType.Workspace, NodeType.Room }
    };
}
