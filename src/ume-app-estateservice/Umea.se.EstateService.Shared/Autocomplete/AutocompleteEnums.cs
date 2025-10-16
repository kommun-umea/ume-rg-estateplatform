using System.Text.Json.Serialization;

namespace Umea.se.EstateService.Shared.Autocomplete;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AutocompleteType
{
    Any,
    Building,
    Room,
    Estate
}

public enum MatchedField
{
    Name,
    PopularName,
    BuildingName,
    Other
}
