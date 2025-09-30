using System.Runtime.CompilerServices;
using Umea.se.EstateService.Logic.Interfaces;
using Umea.se.EstateService.Logic.Mappers;
using Umea.se.EstateService.ServiceAccess.Pythagoras;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Dto;
using Umea.se.EstateService.Shared.Models;

namespace Umea.se.EstateService.Logic.Handlers;

public class PythagorasHandler(IPythagorasClient pythagorasClient) : IPythagorasHandler
{
    private const string BuildingsEndpoint = "rest/v1/building";
    private const string WorkspacesEndpoint = "rest/v1/workspace";
    private const string NavigationFoldersEndpoint = "rest/v1/navigationfolder/info";
    private const int MaxAutocompleteLimit = 1000;
    private static string BuildingWorkspacesEndpoint(int buildingId) => $"rest/v1/building/{buildingId}/workspace/info";

    public async Task<IReadOnlyList<BuildingModel>> GetBuildingsAsync(PythagorasQuery<Building>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Building> payload = await pythagorasClient.GetAsync(BuildingsEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasBuildingMapper.ToModel(payload);
    }

    public async IAsyncEnumerable<BuildingModel> GetPaginatedBuildingsAsync(PythagorasQuery<Building>? query = null, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Building dto in pythagorasClient.GetPaginatedAsync(BuildingsEndpoint, query, pageSize, cancellationToken).ConfigureAwait(false))
        {
            yield return PythagorasBuildingMapper.ToModel(dto);
        }
    }

    public async Task<IReadOnlyList<BuildingWorkspaceModel>> GetBuildingWorkspacesAsync(int buildingId, PythagorasQuery<BuildingWorkspace>? query = null, CancellationToken cancellationToken = default)
    {
        if (buildingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive.");
        }

        string endpoint = BuildingWorkspacesEndpoint(buildingId);
        IReadOnlyList<BuildingWorkspace> payload = await pythagorasClient.GetAsync(endpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async Task<IReadOnlyList<WorkspaceModel>> GetWorkspacesAsync(PythagorasQuery<Workspace>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Workspace> payload = await pythagorasClient.GetAsync(WorkspacesEndpoint, query, cancellationToken).ConfigureAwait(false);
        return PythagorasWorkspaceMapper.ToModel(payload);
    }

    public async IAsyncEnumerable<WorkspaceModel> GetPaginatedWorkspacesAsync(PythagorasQuery<Workspace>? query = null, int pageSize = 50, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Workspace dto in pythagorasClient.GetPaginatedAsync(WorkspacesEndpoint, query, pageSize, cancellationToken).ConfigureAwait(false))
        {
            yield return PythagorasWorkspaceMapper.ToDomain(dto);
        }
    }

    public async Task<IReadOnlyList<EstateModel>> GetEstatesAsync(PythagorasQuery<NavigationFolder>? query = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NavigationFolder> payload = await pythagorasClient
            .GetAsync(NavigationFoldersEndpoint, query, cancellationToken)
            .ConfigureAwait(false);

        return PythagorasEstateMapper.ToModel(payload);
    }

    public Task<IReadOnlyList<BuildingSearchResult>> SearchBuildingsAsync(string searchTerm, int limit = 10, CancellationToken cancellationToken = default)
        => SearchAndMapAsync<Building, BuildingSearchResult>(
            BuildingsEndpoint,
            searchTerm,
            limit,
            PythagorasAutocompleteMapper.ToBuildingResults,
            cancellationToken);

    public async Task<IReadOnlyList<WorkspaceSearchResult>> SearchWorkspacesAsync(string searchTerm, int limit = 10, int? buildingId = null, CancellationToken cancellationToken = default)
    {
        if (buildingId is int id && id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildingId), "Building id must be positive when supplied.");
        }

        if (buildingId is int scopedId)
        {
            return await SearchAndMapAsync<BuildingWorkspace, WorkspaceSearchResult>(
                    BuildingWorkspacesEndpoint(scopedId),
                    searchTerm,
                    limit,
                    PythagorasAutocompleteMapper.ToWorkspaceResults,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await SearchAndMapAsync<Workspace, WorkspaceSearchResult>(
                WorkspacesEndpoint,
                searchTerm,
                limit,
                PythagorasAutocompleteMapper.ToWorkspaceResults,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<TResult>> SearchAndMapAsync<TInput, TResult>(
        string endpoint,
        string searchTerm,
        int limit,
        Func<IReadOnlyList<TInput>, IReadOnlyList<TResult>> mapper,
        CancellationToken cancellationToken) where TInput : class
    {
        ValidateSearchInputs(searchTerm, limit);

        PythagorasQuery<TInput> query = new();
        ApplySearch(query, searchTerm, limit);

        IReadOnlyList<TInput> payload = await pythagorasClient.GetAsync(endpoint, query, cancellationToken).ConfigureAwait(false);

        return mapper(payload);
    }

    private static void ValidateSearchInputs(string searchTerm, int limit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        if (limit <= 0 || limit > MaxAutocompleteLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive.");
        }
    }

    private static void ApplySearch<T>(PythagorasQuery<T> query, string searchTerm, int limit) where T : class
    {
        query.GeneralSearch(searchTerm);
        query.Take(limit);
    }
}
