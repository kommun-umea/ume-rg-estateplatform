using Umea.se.EstateService.Logic.Models;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

namespace Umea.se.EstateService.API.Requests;

/// <summary>
/// Extension helpers for translating <see cref="PagedQueryRequest"/> into query types.
/// </summary>
public static class PagedQueryRequestExtensions
{
    /// <summary>
    /// Converts a paged query request to <see cref="QueryArgs"/> for DataStore queries.
    /// </summary>
    public static QueryArgs ToQueryArgs(this PagedQueryRequest request) => QueryArgs.Create(
        skip: request.Offset > 0 ? request.Offset : null,
        take: request.Limit > 0 ? request.Limit : null,
        searchTerm: request.SearchTerm);

    /// <summary>
    /// Applies paging information from the request onto the provided query.
    /// </summary>
    /// <typeparam name="T">The Pythagoras DTO type.</typeparam>
    /// <param name="query">Existing query to augment.</param>
    /// <param name="request">The request providing limit and offset.</param>
    /// <returns>The query instance with paging applied.</returns>
    public static PythagorasQuery<T> ApplyPaging<T>(
        this PythagorasQuery<T> query,
        PagedQueryRequest request) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(request);

        PythagorasQuery<T> updated = query;

        if (request.Offset > 0)
        {
            updated = updated.Skip(request.Offset);
        }

        if (request.Limit > 0)
        {
            updated = updated.Take(request.Limit);
        }

        return updated;
    }

    /// <summary>
    /// Applies a general search when a search term is provided.
    /// </summary>
    /// <typeparam name="T">The Pythagoras DTO type.</typeparam>
    /// <param name="query">Existing query to augment.</param>
    /// <param name="request">The request providing the search term.</param>
    /// <returns>The query instance with a general search applied when available.</returns>
    public static PythagorasQuery<T> ApplyGeneralSearch<T>(
        this PythagorasQuery<T> query,
        PagedQueryRequest request) where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return query;
        }

        return query.GeneralSearch(request.SearchTerm.Trim());
    }
}
