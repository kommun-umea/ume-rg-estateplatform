namespace Umea.se.EstateService.Logic.Search;

public sealed record QueryOptions(
    bool EnablePrefix = true,
    bool EnableFuzzy = true,
    int FuzzyMaxEdits = 1,
    int MaxResults = 20,
    bool PreferEstatesOnTie = true
);
