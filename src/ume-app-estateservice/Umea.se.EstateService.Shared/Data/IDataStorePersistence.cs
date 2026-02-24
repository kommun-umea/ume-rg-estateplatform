namespace Umea.se.EstateService.Shared.Data;

public interface IDataStorePersistence
{
    Task<(DataSnapshot? Snapshot, DateTimeOffset? LastRefresh)> TryLoadAsync(CancellationToken ct = default);
    Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default);
}
