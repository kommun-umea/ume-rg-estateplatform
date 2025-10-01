using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Search;

public sealed class PythagorasDocument
{
    public string Id { get; set; } = default!;
    public NodeType Type { get; set; }
    public string Name { get; set; } = default!;
    public string? PopularName { get; set; }
    public string? Address { get; set; }
    public List<string>? Aliases { get; set; }
    public List<Ancestor> Ancestors { get; set; } = [];
    public string? Path => string.Join(" > ", Ancestors.Select(a => a.Name + " " + a.PopularName).Append(Name + " " + PopularName));
    [JsonPropertyName("_geo")]
    public GeoPoint? Geo { get; set; }
    public string? ThumbnailUrl { get; set; }
    [JsonIgnore]
    public double RankScore { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Slug { get; set; }
}

