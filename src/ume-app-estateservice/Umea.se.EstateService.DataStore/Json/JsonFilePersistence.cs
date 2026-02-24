using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umea.se.EstateService.Shared.Data;
using Umea.se.EstateService.Shared.Data.Entities;

namespace Umea.se.EstateService.DataStore.Json;

/// <summary>
/// JSON file-based persistence for the data store.
/// Implements IDataStorePersistence to save and load snapshots from a JSON file.
/// </summary>
public sealed class JsonFilePersistence(
    IOptions<JsonFilePersistenceOptions> options,
    ILogger<JsonFilePersistence> logger) : IDataStorePersistence
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false, // Smaller file size for production
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _cacheFilePath = options.Value.CacheFilePath;
    private readonly ILogger<JsonFilePersistence> _logger = logger;

    public Task<(DataSnapshot? Snapshot, DateTimeOffset? LastRefresh)> TryLoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return Task.FromResult<(DataSnapshot?, DateTimeOffset?)>((null, null));
            }

            _logger.LogInformation("Loading data store snapshot from {Path}", _cacheFilePath);

            string json = File.ReadAllText(_cacheFilePath);
            JsonCacheSnapshot? snapshot = JsonSerializer.Deserialize<JsonCacheSnapshot>(json, s_jsonOptions);

            if (snapshot is null)
            {
                _logger.LogWarning("Failed to deserialize data store snapshot from {Path}", _cacheFilePath);
                return Task.FromResult<(DataSnapshot?, DateTimeOffset?)>((null, null));
            }

            // Build the snapshot
            DataSnapshot dataSnapshot = new(
                estates: [.. snapshot.Estates],
                buildings: [.. snapshot.Buildings],
                floors: [.. snapshot.Floors],
                rooms: [.. snapshot.Rooms],
                buildingAscendants: snapshot.BuildingAscendants ?? ImmutableDictionary<int, BuildingAscendantTriplet>.Empty,
                refreshUtc: snapshot.LastRefreshUtc
            );

            _logger.LogInformation(
                "Loaded data store snapshot with {EstateCount} estates and {BuildingCount} buildings from {Path}",
                dataSnapshot.Estates.Length, dataSnapshot.Buildings.Length, _cacheFilePath);

            return Task.FromResult<(DataSnapshot?, DateTimeOffset?)>((dataSnapshot, snapshot.LastRefreshUtc));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while loading data store snapshot from {Path}", _cacheFilePath);
            return Task.FromResult<(DataSnapshot?, DateTimeOffset?)>((null, null));
        }
    }

    public Task SaveAsync(DataSnapshot snapshot, DateTimeOffset refreshTime, CancellationToken ct = default)
    {
        try
        {
            if (!snapshot.IsReady)
            {
                return Task.CompletedTask;
            }

            JsonCacheSnapshot cacheSnapshot = new()
            {
                LastRefreshUtc = refreshTime,
                Estates = [.. snapshot.Estates],
                Buildings = [.. snapshot.Buildings],
                Floors = [.. snapshot.Floors],
                Rooms = [.. snapshot.Rooms],
                BuildingAscendants = snapshot.BuildingAscendants.ToDictionary(kv => kv.Key, kv => kv.Value)
            };

            string? directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(cacheSnapshot, s_jsonOptions);
            string tempPath = _cacheFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _cacheFilePath, overwrite: true);

            _logger.LogInformation("Saved data store snapshot to {Path}", _cacheFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving data store snapshot to {Path}", _cacheFilePath);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration options for JsonFilePersistence.
/// </summary>
public sealed class JsonFilePersistenceOptions
{
    public string CacheFilePath { get; set; } = "DataStoreCache/estate-data.json";
}

/// <summary>
/// Serializable snapshot for persisting data store state to JSON.
/// Uses record type with init-only properties for defensive immutability.
/// </summary>
internal sealed record JsonCacheSnapshot
{
    public required DateTimeOffset LastRefreshUtc { get; init; }
    public required IReadOnlyList<EstateEntity> Estates { get; init; }
    public required IReadOnlyList<BuildingEntity> Buildings { get; init; }
    public required IReadOnlyList<FloorEntity> Floors { get; init; }
    public required IReadOnlyList<RoomEntity> Rooms { get; init; }
    public IReadOnlyDictionary<int, BuildingAscendantTriplet>? BuildingAscendants { get; init; }
}
