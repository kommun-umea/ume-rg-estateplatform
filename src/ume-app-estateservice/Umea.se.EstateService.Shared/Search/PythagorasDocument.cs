using System.Net.Sockets;
using System.Text.Json.Serialization;
using Umea.se.EstateService.Shared.ValueObjects;

namespace Umea.se.EstateService.Shared.Search;

public sealed class PythagorasDocument
{
    public string Id { get; set; } = default!;
    public NodeType Type { get; set; }
    public string Name { get; set; } = default!;
    public AddressModel? Address { get; set; }
    public List<string>? Aliases { get; set; }
    public List<Ancestor> Ancestors { get; set; } = [];
    [JsonPropertyName("_geo")]
    public GeoPoint? Geo { get; set; }
    public string? ThumbnailUrl { get; set; }
    public double RankScore { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Slug { get; set; }
}

