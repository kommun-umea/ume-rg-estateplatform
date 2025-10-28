namespace Umea.se.EstateService.Logic.Options;

public sealed class SearchOptions
{
    public const string SectionName = "SearchIndex";

    /// <summary>
    /// When true, room documents are excluded from the search index.
    /// </summary>
    public bool ExcludeRooms { get; init; }
}
