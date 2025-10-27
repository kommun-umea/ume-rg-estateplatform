using System.Text.Json.Serialization;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

public sealed class BuildingUiListDataRequest
{
    public IReadOnlyCollection<int>? BuildingIds { get; init; }
    public IReadOnlyCollection<int>? PropertyIds { get; init; }
    public int? NavigationId { get; init; }
    public bool IncludePropertyValues { get; init; } = true;
}

public sealed class NavigationFolderUiListDataRequest
{
    public IReadOnlyCollection<int>? NavigationFolderIds { get; init; }
    public IReadOnlyCollection<int>? PropertyIds { get; init; }
    public int? NavigationId { get; init; }
    public bool IncludePropertyValues { get; init; } = true;
}

public sealed class UiListDataResponse<TItem>
{
    [JsonPropertyName("data")]
    public List<TItem> Data { get; init; } = [];

    [JsonPropertyName("totalSize")]
    public int TotalSize { get; init; }
}
