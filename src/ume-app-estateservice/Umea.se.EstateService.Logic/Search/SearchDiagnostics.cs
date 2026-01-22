using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Search;

/// <summary>
/// Complete diagnostic information for a search operation.
/// </summary>
public sealed record SearchDiagnostics
{
    public required string OriginalQuery { get; init; }
    public required string NormalizedQuery { get; init; }
    public required string[] QueryTokens { get; init; }
    public required Dictionary<string, TokenExpansion> TokenExpansions { get; init; }
    public required int CandidateDocumentCount { get; init; }
    public required int FilteredDocumentCount { get; init; }
    public required List<DocumentScoreBreakdown> TopScoreBreakdowns { get; init; }
    public required QueryOptionsSnapshot AppliedOptions { get; init; }
    public required long ElapsedMilliseconds { get; init; }
}

/// <summary>
/// Shows how a single query token was expanded into candidate index terms.
/// </summary>
public sealed record TokenExpansion
{
    public required string QueryToken { get; init; }
    public required List<string> ExactMatches { get; init; }
    public required List<string> PrefixMatches { get; init; }
    public required List<string> FuzzyMatches { get; init; }
    public required List<string> NgramMatches { get; init; }
    public required int TotalCandidates { get; init; }
}

/// <summary>
/// Detailed score breakdown for a single document.
/// </summary>
public sealed record DocumentScoreBreakdown
{
    public required int DocId { get; init; }
    public required string Name { get; init; }
    public required string? PopularName { get; init; }
    public required NodeType Type { get; init; }
    public required double TypeBoost { get; init; }
    public required double Bm25Score { get; init; }
    public required double ExactMatchBonus { get; init; }
    public required double StartsWithBonus { get; init; }
    public required double PopularStartsWithBonus { get; init; }
    public required double ProximityBonus { get; init; }
    public required double TermMatchBonus { get; init; }
    public required double GeoBonus { get; init; }
    public required double ExactTokenMatchBonus { get; init; }
    public required double SameFieldMultiTokenBonus { get; init; }
    public required double NgramDedupeAdjustment { get; init; }
    public required double FinalScore { get; init; }
    public required List<FieldHitInfo> FieldHits { get; init; }
    public required Dictionary<string, string> MatchedTerms { get; init; }
}

/// <summary>
/// Information about a term match in a specific field.
/// </summary>
public sealed record FieldHitInfo
{
    public required string Field { get; init; }
    public required string MatchedTerm { get; init; }
    public required string QueryToken { get; init; }
    public required int[] Positions { get; init; }
    public required double FieldWeight { get; init; }
    public required double PositionWeight { get; init; }
    public required double ContributedScore { get; init; }
}

/// <summary>
/// Snapshot of the query options that were applied.
/// </summary>
public sealed record QueryOptionsSnapshot
{
    public required bool EnablePrefix { get; init; }
    public required bool EnableFuzzy { get; init; }
    public required bool EnableContains { get; init; }
    public required int FuzzyMaxEdits { get; init; }
    public required int MaxResults { get; init; }
    public required bool PreferEstatesOnTie { get; init; }
    public required List<string>? FilterByTypes { get; init; }
    public required List<int>? FilterByBusinessTypes { get; init; }
    public required string? GeoFilterDescription { get; init; }
}
