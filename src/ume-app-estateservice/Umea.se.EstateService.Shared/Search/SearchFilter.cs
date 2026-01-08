using Umea.se.EstateService.Shared.Autocomplete;

namespace Umea.se.EstateService.Shared.Search;

public sealed class SearchFilter
{
    public IReadOnlyCollection<AutocompleteType> Types { get; set; } = Array.Empty<AutocompleteType>();
    public IReadOnlyList<int> BusinessTypeIds { get; set; } = [];
}
