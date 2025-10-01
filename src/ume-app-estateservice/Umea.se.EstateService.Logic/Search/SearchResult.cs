using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Search;

public sealed record SearchResult(PythagorasDocument Item, double Score, IReadOnlyDictionary<string, string> MatchedTerms);
