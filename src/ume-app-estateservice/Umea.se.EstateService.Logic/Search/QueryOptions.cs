using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Search;

public sealed record QueryOptions(
    bool EnablePrefix = true,
    bool EnableFuzzy = true,
    int FuzzyMaxEdits = 1,
    int MaxResults = 50,
    bool PreferEstatesOnTie = true,
    IReadOnlyCollection<NodeType>? FilterByTypes = null,
    bool EnableContains = true,
    GeoFilter? GeoFilter = null
);
