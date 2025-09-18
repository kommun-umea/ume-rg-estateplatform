using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Umea.se.EstateService.API.Controllers;
using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.Shared.Autocomplete;

namespace Umea.se.EstateService.API.Services;

public interface IAutocompleteService
{
    Task<AutocompleteResponse> SearchAsync(AutocompleteRequest request, CancellationToken cancellationToken);
}

public sealed class AutocompleteService(IPythagorasService pythagorasService, IMemoryCache cache) : IAutocompleteService
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);

    public async Task<AutocompleteResponse> SearchAsync(AutocompleteRequest request, CancellationToken cancellationToken)
    {
        string cacheKey = BuildCacheKey(request);
        if (cache.TryGetValue(cacheKey, out AutocompleteResponse? cached) && cached is not null)
        {
            return cached;
        }

        Task<IReadOnlyList<BuildingSearchResult>>? buildingTask = null;
        Task<IReadOnlyList<WorkspaceSearchResult>>? workspaceTask = null;
        List<Task> inflight = [];

        const int fetchLimit = AutocompleteRequest.MaxLimit;

        if (request.Type is AutocompleteType.Building or AutocompleteType.Any)
        {
            buildingTask = pythagorasService.SearchBuildingsAsync(request.Query, fetchLimit, cancellationToken);
            inflight.Add(buildingTask);
        }

        if (request.Type is AutocompleteType.Workspace or AutocompleteType.Any)
        {
            workspaceTask = pythagorasService.SearchWorkspacesAsync(request.Query, fetchLimit, request.BuildingId, cancellationToken);
            inflight.Add(workspaceTask);
        }

        if (inflight.Count > 0)
        {
            await Task.WhenAll(inflight);
        }

        List<AutocompleteItemModel> collected = [];

        if (buildingTask is not null)
        {
            IReadOnlyList<BuildingSearchResult> buildingResults = await buildingTask;
            collected.AddRange(CreateBuildingItems(buildingResults, request.Query));
        }

        if (workspaceTask is not null)
        {
            IReadOnlyList<WorkspaceSearchResult> workspaceResults = await workspaceTask;
            collected.AddRange(CreateWorkspaceItems(workspaceResults, request.Query));
        }

        List<AutocompleteItemModel> merged = Merge(collected, request.Query, request.Limit);

        AutocompleteResponse response = new()
        {
            Items = merged
        };

        cache.Set(cacheKey, response, _cacheDuration);
        return response;
    }

    private static IEnumerable<AutocompleteItemModel> CreateBuildingItems(IEnumerable<BuildingSearchResult> results, string searchTerm)
    {
        string normalizedQuery = Normalize(searchTerm);

        foreach (BuildingSearchResult result in results)
        {
            MatchedField matchedField = DetermineMatchedField(
                normalizedQuery,
                (MatchedField.Name, result.Name),
                (MatchedField.PopularName, result.PopularName));

            yield return new AutocompleteItemModel
            {
                Type = AutocompleteType.Building,
                Id = result.Id,
                Uid = result.Uid,
                Name = result.Name,
                PopularName = result.PopularName,
                MatchedField = matchedField
            };
        }
    }

    private static IEnumerable<AutocompleteItemModel> CreateWorkspaceItems(IEnumerable<WorkspaceSearchResult> results, string searchTerm)
    {
        string normalizedQuery = Normalize(searchTerm);

        foreach (WorkspaceSearchResult result in results)
        {
            MatchedField matchedField = DetermineMatchedField(
                normalizedQuery,
                (MatchedField.Name, result.Name),
                (MatchedField.PopularName, result.PopularName),
                (MatchedField.BuildingName, result.BuildingName));

            yield return new AutocompleteItemModel
            {
                Type = AutocompleteType.Workspace,
                Id = result.Id,
                Uid = result.Uid,
                BuildingId = result.BuildingId,
                BuildingName = result.BuildingName,
                Name = result.Name,
                PopularName = result.PopularName,
                MatchedField = matchedField
            };
        }
    }

    private static List<AutocompleteItemModel> Merge(IEnumerable<AutocompleteItemModel> results, string query, int limit)
    {
        Dictionary<(AutocompleteType Type, int Id), AutocompleteItemModel> unique = [];

        foreach (AutocompleteItemModel item in results)
        {
            unique.TryAdd((item.Type, item.Id), item);
        }

        string normalizedQuery = Normalize(query);

        return [.. unique.Values
            .Select(item => new { Item = item, Score = Score(item, normalizedQuery) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Type)
            .ThenBy(x => x.Item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.Item)];
    }

    private static MatchedField DetermineMatchedField(string normalizedQuery, params (MatchedField Field, string? Value)[] candidates)
    {
        foreach ((MatchedField field, string? value) in candidates)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Normalize(value).Contains(normalizedQuery, StringComparison.Ordinal))
            {
                return field;
            }
        }

        return candidates.Length > 0 ? candidates[0].Field : MatchedField.Other;
    }

    private static int Score(AutocompleteItemModel item, string normalizedQuery)
    {
        int baseScore = GetBaseScore(item.MatchedField);
        int positionScore = GetPositionScore(item, normalizedQuery);
        int lengthPenalty = GetLengthPenalty(item);

        return baseScore + positionScore - lengthPenalty;
    }

    private static int GetBaseScore(MatchedField matchedField) => matchedField switch
    {
        MatchedField.Name => 200,
        MatchedField.PopularName => 150,
        MatchedField.BuildingName => 120,
        _ => 100
    };

    private static string GetTargetString(AutocompleteItemModel item) => item.MatchedField switch
    {
        MatchedField.PopularName => item.PopularName,
        MatchedField.BuildingName => item.BuildingName,
        _ => item.Name
    } ?? string.Empty;

    private static int GetPositionScore(AutocompleteItemModel item, string normalizedQuery)
    {
        string target = GetTargetString(item);
        int position = Normalize(target).IndexOf(normalizedQuery, StringComparison.Ordinal);
        return position >= 0 ? Math.Max(40 - position, 0) : 0;
    }

    private static int GetLengthPenalty(AutocompleteItemModel item)
        => Math.Min(item.Name?.Length ?? 0, 40);

    private static string BuildCacheKey(AutocompleteRequest request)
    {
        string type = request.Type switch
        {
            AutocompleteType.Building => "building",
            AutocompleteType.Workspace => "workspace",
            _ => "any"
        };

        string buildingSegment = request.BuildingId.HasValue
            ? request.BuildingId.Value.ToString(CultureInfo.InvariantCulture)
            : "-*";

        string normalizedQuery = Normalize(request.Query);

        return $"autocomplete:{type}:{buildingSegment}:{request.Limit}:{normalizedQuery}";
    }

    private static string Normalize(string? value) => value?.Trim().ToLower(CultureInfo.InvariantCulture) ?? string.Empty;
}
