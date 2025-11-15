namespace Umea.se.EstateService.Logic.Data;

/// <summary>
/// Service responsible for refreshing the in-memory data store with data from Pythagoras API.
/// </summary>
public interface IDataRefreshService
{
    /// <summary>
    /// Fetches data from Pythagoras API, maps it to entities, and updates the data store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshDataAsync(CancellationToken cancellationToken = default);
}
