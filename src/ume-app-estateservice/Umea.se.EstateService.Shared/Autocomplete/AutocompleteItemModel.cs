using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Autocomplete;

public sealed class AutocompleteItemModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AutocompleteType Type { get; init; }

    public int Id { get; init; }

    public Guid? Uid { get; init; }

    public int? BuildingId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? PopularName { get; init; }

    public string? BuildingName { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MatchedField MatchedField { get; init; } = MatchedField.Other;
}
