using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras;

internal static class PythagorasAutocompleteMapper
{
    public static IReadOnlyList<BuildingSearchResult> ToBuildingResults(IReadOnlyList<Building> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return [];
        }

        return [.. items
            .Select(dto => new BuildingSearchResult
            {
                Id = dto.Id,
                Uid = dto.Uid,
                Name = dto.Name ?? string.Empty,
                PopularName = dto.PopularName
            })];
    }

    public static IReadOnlyList<WorkspaceSearchResult> ToWorkspaceResults(IReadOnlyList<BuildingWorkspace> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return [];
        }

        return [.. items
            .Select(dto => new WorkspaceSearchResult
            {
                Id = dto.Id,
                BuildingId = dto.BuildingId,
                Uid = dto.Uid,
                Name = dto.Name ?? string.Empty,
                PopularName = dto.PopularName,
                BuildingName = dto.BuildingName,
            })];
    }

    public static IReadOnlyList<WorkspaceSearchResult> ToWorkspaceResults(IReadOnlyList<Workspace> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return [];
        }

        return [.. items
            .Select(dto => new WorkspaceSearchResult
            {
                Id = dto.Id,
                Uid = dto.Uid,
                Name = dto.Name ?? string.Empty,
                PopularName = dto.PopularName
            })];
    }
}
