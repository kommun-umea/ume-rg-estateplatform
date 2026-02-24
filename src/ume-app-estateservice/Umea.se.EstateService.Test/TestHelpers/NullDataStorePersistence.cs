using Umea.se.EstateService.Shared.Data;

namespace Umea.se.EstateService.Test.TestHelpers;

/// <summary>
/// No-op data store persistence for use in integration tests.
/// </summary>
public sealed class NullDataStorePersistence : IDataStorePersistence
{
    public Task<(DataSnapshot? Snapshot, DateTimeOffset? LastRefresh)> TryLoadAsync(CancellationToken ct = default)
    {
        return Task.FromResult<(DataSnapshot?, DateTimeOffset?)>((null, null));
    }

    public Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
