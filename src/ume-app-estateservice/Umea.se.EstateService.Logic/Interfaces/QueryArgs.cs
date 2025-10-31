namespace Umea.se.EstateService.Logic.Interfaces;

/// <summary>
/// Generic query options for filtering, paging, and sorting collection results.
/// Can be mapped to PythagorasQuery for API calls or LINQ expressions for DataStore queries.
/// </summary>
public record QueryArgs
{
    /// <summary>
    /// Paging configuration.
    /// </summary>
    public PagingOptions? Paging { get; init; }

    /// <summary>
    /// General search term applied across searchable fields.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Optional ordering specification.
    /// </summary>
    public OrderingOptions? Ordering { get; init; }

    /// <summary>
    /// Creates query options with only paging specified.
    /// </summary>
    public static QueryArgs WithPaging(int? skip = null, int? take = null) => new()
    {
        Paging = skip.HasValue || take.HasValue
            ? new PagingOptions { Skip = skip, Take = take }
            : null
    };

    /// <summary>
    /// Creates query options with search term.
    /// </summary>
    public static QueryArgs WithSearch(string searchTerm) => new()
    {
        SearchTerm = searchTerm
    };

    /// <summary>
    /// Creates query options with both paging and search.
    /// </summary>
    public static QueryArgs Create(int? skip = null, int? take = null, string? searchTerm = null) => new()
    {
        Paging = skip.HasValue || take.HasValue
            ? new PagingOptions { Skip = skip, Take = take }
            : null,
        SearchTerm = searchTerm
    };
}

/// <summary>
/// Paging configuration using offset-based pagination.
/// </summary>
public record PagingOptions
{
    /// <summary>
    /// Number of items to skip (offset).
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// Maximum number of items to return (limit).
    /// </summary>
    public int? Take { get; init; }

    /// <summary>
    /// Validates paging options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when Skip is negative or Take is zero or negative.</exception>
    public void Validate()
    {
        if (Skip.HasValue && Skip.Value < 0)
        {
            throw new ArgumentException("Skip must be >= 0", nameof(Skip));
        }

        if (Take.HasValue && Take.Value <= 0)
        {
            throw new ArgumentException("Take must be > 0", nameof(Take));
        }
    }
}

/// <summary>
/// Ordering/sorting configuration.
/// </summary>
public record OrderingOptions
{
    /// <summary>
    /// Field name to order by (e.g., "Name", "Id").
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Sort direction.
    /// </summary>
    public SortingDirection Direction { get; init; } = SortingDirection.Ascending;
}

/// <summary>
/// Sort direction for ordering results.
/// </summary>
public enum SortingDirection
{
    /// <summary>
    /// Sort in ascending order (A-Z, 0-9, oldest-newest).
    /// </summary>
    Ascending,

    /// <summary>
    /// Sort in descending order (Z-A, 9-0, newest-oldest).
    /// </summary>
    Descending
}
