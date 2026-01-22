using Umea.se.EstateService.Logic.Search;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.API.Controllers.Responses;

/// <summary>
/// Response containing search results with full diagnostic information.
/// </summary>
public sealed record SearchDebugResponse
{
    /// <summary>
    /// The search results.
    /// </summary>
    public required IReadOnlyList<PythagorasDocument> Results { get; init; }

    /// <summary>
    /// Full diagnostic information about the search operation.
    /// </summary>
    public required SearchDiagnostics Diagnostics { get; init; }
}
