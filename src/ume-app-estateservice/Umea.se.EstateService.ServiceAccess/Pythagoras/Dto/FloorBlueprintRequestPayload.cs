using System.Globalization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed record FloorBlueprintRequestPayload
{
    public string PageSize { get; init; } = "A4";
    public string PageOrientation { get; init; } = "PORTRAIT";
    public string PageScale { get; init; } = "BEST_FIT";
    public int X { get; init; }
    public int Y { get; init; }
    public string StampId { get; init; } = "stamp1";
    public IReadOnlyList<string> ComponentTypeGroupNamesToExclude { get; init; } = [];
    public IReadOnlyList<int> ComponentTypeIdsToExclude { get; init; } = [];
    public IReadOnlyList<int> ComponentIdsToExclude { get; init; } = [];
    public IReadOnlyList<int> ComponentPointerIdsToExclude { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>> WorkspaceTexts { get; init; }
        = new Dictionary<string, IReadOnlyList<string>>();
    public IReadOnlyDictionary<string, string> WorkspaceColors { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> WorkspaceFillPatterns { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> ColorKey { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> ColorKeyFillPatterns { get; init; } = new Dictionary<string, string>();
    public string? ColorKeyHeader { get; init; }

    public static FloorBlueprintRequestPayload CreateDefault() => new();

    public FloorBlueprintRequestPayload WithWorkspaceTexts(IDictionary<int, IReadOnlyList<string>> workspaceTexts)
    {
        Dictionary<string, IReadOnlyList<string>> converted = workspaceTexts.ToDictionary(
            static kvp => kvp.Key.ToString(CultureInfo.InvariantCulture),
            static kvp => kvp.Value);

        return this with { WorkspaceTexts = converted };
    }
}
