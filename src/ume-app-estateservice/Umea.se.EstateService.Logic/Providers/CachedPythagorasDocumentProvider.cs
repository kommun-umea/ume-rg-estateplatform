using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Providers;

public class CachedPythagorasDocumentProvider(PythagorasDocumentProvider pythagorasDocumentProvider) : IPythagorasDocumentProvider
{
    private static readonly object CacheLock = new();
    private static ICollection<PythagorasDocument>? _cachedDocuments;
    private static Task<ICollection<PythagorasDocument>>? _cacheRefreshTask;

    public Task<ICollection<PythagorasDocument>> GetDocumentsAsync()
    {
        ICollection<PythagorasDocument>? cached = Volatile.Read(ref _cachedDocuments);
        if (cached != null)
        {
            return Task.FromResult(cached);
        }

        return EnsureCacheRefreshTask();

        Task<ICollection<PythagorasDocument>> EnsureCacheRefreshTask()
        {
            lock (CacheLock)
            {
                if (_cachedDocuments is { } existing)
                {
                    return Task.FromResult(existing);
                }

                _cacheRefreshTask ??= RefreshCacheAsync();
                return _cacheRefreshTask;
            }
        }
    }

    private async Task<ICollection<PythagorasDocument>> RefreshCacheAsync()
    {
        try
        {
            ICollection<PythagorasDocument> documents = await pythagorasDocumentProvider.GetDocumentsAsync().ConfigureAwait(false);

            lock (CacheLock)
            {
                _cachedDocuments = documents;
            }

            return documents;
        }
        finally
        {
            lock (CacheLock)
            {
                _cacheRefreshTask = null;
            }
        }
    }
}
