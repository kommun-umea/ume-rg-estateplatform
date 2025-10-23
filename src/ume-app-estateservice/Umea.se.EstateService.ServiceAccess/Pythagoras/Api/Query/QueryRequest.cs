
using System.Collections.Immutable;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Query;

public sealed record QueryRequest
{
    public ImmutableList<int> Ids { get; init; } = [];
    public string? GeneralSearch { get; init; }
    public ImmutableList<Filter> Filters { get; init; } = [];
    public Order? OrderBy { get; init; }
    public Paging? Page { get; init; }
    public ImmutableList<KeyValuePair<string, string>> AdditionalParameters { get; init; } = [];
}
