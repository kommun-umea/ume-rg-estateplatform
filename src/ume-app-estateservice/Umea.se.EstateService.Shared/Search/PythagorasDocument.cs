using System.Text.Json.Serialization;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Search;

public sealed class PythagorasDocument
{
    public readonly record struct DocumentKey(NodeType Type, int Id);

    public int Id { get; set; }
    public NodeType Type { get; set; }
    public string Name { get; set; } = default!;
    public string? PopularName { get; set; }
    public AddressModel? Address { get; set; }
    [JsonIgnore]
    public string? AddressSearchText => FormatAddress(Address);
    public List<string>? Aliases { get; set; }
    public List<Ancestor> Ancestors { get; set; } = [];
    public string? Path => string.Join(" > ", Ancestors.Select(a => a.Name + " " + a.PopularName).Append(Name + " " + PopularName));
    [JsonPropertyName("_geo")]
    public GeoPoint? LegacyGeo
    {
        get => GeoLocation;
        set => GeoLocation = value;
    }
    public GeoPoint? GeoLocation { get; set; }
    public string? ThumbnailUrl { get; set; }
    [JsonIgnore]
    public double RankScore { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Slug { get; set; }
    /// <summary>
    /// Number of children for the entity, if available.
    /// </summary>
    public int NumChildren { get; set; }
    /// <summary>
    /// Total area for the entity, if available.
    /// </summary>
    public decimal? GrossArea { get; set; }
    /// <summary>
    /// Number of distinct floors associated with the entity, if available.
    /// </summary>
    public int? NumFloors { get; set; }
    /// <summary>
    /// Number of rooms/workspaces associated with the entity, if available.
    /// </summary>
    public int? NumRooms { get; set; }
    public IReadOnlyDictionary<string, string>? ExtendedProperties { get; set; }
    [JsonIgnore]
    public DocumentKey Key => new(Type, Id);

    private static string? FormatAddress(AddressModel? address)
    {
        if (address is null)
        {
            return null;
        }

        string[] parts =
        [
            address.Street,
            address.ZipCode,
            address.City
        ];

        string formatted = string.Join(' ', parts.Where(static p => !string.IsNullOrWhiteSpace(p)));
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }
}
