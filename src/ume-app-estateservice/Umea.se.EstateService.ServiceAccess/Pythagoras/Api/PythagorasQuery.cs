using System.Linq.Expressions;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Enum;
using Umea.se.EstateService.ServiceAccess.Pythagoras.Helpers;

namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;
public enum FieldTarget { Parameter, Attribute }

public class PythagorasQuery<T> where T : class
{
    private readonly QueryRequest _req;
    private readonly bool _usedSkip;
    private readonly bool _usedTake;
    private readonly bool _usedPage;

    public PythagorasQuery()
        : this(new QueryRequest(), usedSkip: false, usedTake: false, usedPage: false)
    {
    }

    private PythagorasQuery(QueryRequest req, bool usedSkip, bool usedTake, bool usedPage)
    {
        _req = req;
        _usedSkip = usedSkip;
        _usedTake = usedTake;
        _usedPage = usedPage;
    }

    // ---- Public API ----

    public PythagorasQuery<T> WithIds(params int[] ids)
    {
        if (ids is not { Length: > 0 })
        {
            return this;
        }

        QueryRequest newReq = _req with { Ids = _req.Ids.AddRange(ids) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> GeneralSearch(string value)
    {
        QueryRequest newReq = _req with { GeneralSearch = value };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> WithQueryParameter<TValue>(string name, TValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            throw new ArgumentException("Parameter name must contain characters.", nameof(name));
        }

        if (QueryStringWriter.IsReservedKey(trimmedName))
        {
            throw new ArgumentException($"The parameter name '{trimmedName}' is reserved and cannot be used.", nameof(name));
        }

        string formattedValue = PythagorasQueryHelpers<T>.FormatValue(value);
        QueryRequest newReq = _req with { AdditionalParameters = _req.AdditionalParameters.SetItem(trimmedName, formattedValue) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> Where<TProp>(Expression<Func<T, TProp>> selector, Op op, TProp value, FieldTarget target = FieldTarget.Parameter)
    {
        string name = PythagorasQueryHelpers<T>.GetApiName(selector);
        string str = PythagorasQueryHelpers<T>.FormatValue(value);
        Filter filter = new(target, name, op, str);
        QueryRequest newReq = _req with { Filters = _req.Filters.Add(filter) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> Where<TProp>(Expression<Func<T, TProp>> selector, TProp value, FieldTarget target = FieldTarget.Parameter)
        => Where(selector, Op.Eq, value, target);

    public PythagorasQuery<T> Contains(Expression<Func<T, string>> s, string v, bool caseSensitive = false)
        => Where(s, caseSensitive ? Op.LikeAnywhere : Op.ILikeAnywhere, v);

    public PythagorasQuery<T> StartsWith(Expression<Func<T, string>> s, string v, bool caseSensitive = false)
        => Where(s, caseSensitive ? Op.LikeStarts : Op.ILikeStarts, v);

    public PythagorasQuery<T> EndsWith(Expression<Func<T, string>> s, string v, bool caseSensitive = false)
        => Where(s, caseSensitive ? Op.LikeEnds : Op.ILikeEnds, v);

    public PythagorasQuery<T> ContainsAttribute(Expression<Func<T, string>> s, string v, bool caseSensitive = false)
        => Where(s, caseSensitive ? Op.LikeAnywhere : Op.ILikeAnywhere, v, FieldTarget.Attribute);

    public PythagorasQuery<T> StartsWithAttribute(Expression<Func<T, string>> s, string v, bool caseSensitive = false)
        => Where(s, caseSensitive ? Op.LikeStarts : Op.ILikeStarts, v, FieldTarget.Attribute);

    public PythagorasQuery<T> EndsWithAttribute(Expression<Func<T, string>> s, string v, bool caseSensitive = false)
        => Where(s, caseSensitive ? Op.LikeEnds : Op.ILikeEnds, v, FieldTarget.Attribute);

    public PythagorasQuery<T> GreaterThan<TProp>(Expression<Func<T, TProp>> s, TProp v)
        => Where(s, Op.Gt, v);

    public PythagorasQuery<T> GreaterThanOrEqual<TProp>(Expression<Func<T, TProp>> s, TProp v)
        => Where(s, Op.Ge, v);

    public PythagorasQuery<T> LessThan<TProp>(Expression<Func<T, TProp>> s, TProp v)
        => Where(s, Op.Lt, v);

    public PythagorasQuery<T> LessThanOrEqual<TProp>(Expression<Func<T, TProp>> s, TProp v)
        => Where(s, Op.Le, v);

    public PythagorasQuery<T> NotEqual<TProp>(Expression<Func<T, TProp>> s, TProp v)
        => Where(s, Op.Ne, v);

    public PythagorasQuery<T> Between<TProp>(Expression<Func<T, TProp>> s, TProp min, TProp max, FieldTarget target = FieldTarget.Parameter)
    {
        string name = PythagorasQueryHelpers<T>.GetApiName(s);
        Filter minFilter = new(target, name, Op.Ge, PythagorasQueryHelpers<T>.FormatValue(min));
        Filter maxFilter = new(target, name, Op.Le, PythagorasQueryHelpers<T>.FormatValue(max));

        QueryRequest newReq = _req with { Filters = _req.Filters.AddRange([minFilter, maxFilter]) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> WhereAttribute<TProp>(Expression<Func<T, TProp>> s, string op, TProp v)
        => Where(s, OperatorMaps.FromStringOrPrefix(op), v, FieldTarget.Attribute);

    public PythagorasQuery<T> WhereParameter<TProp>(Expression<Func<T, TProp>> s, string op, TProp v)
        => Where(s, OperatorMaps.FromStringOrPrefix(op), v, FieldTarget.Parameter);

    public PythagorasQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> s)
    {
        QueryRequest newReq = _req with { OrderBy = new Order(PythagorasQueryHelpers<T>.GetApiName(s), true) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> OrderByDescending<TProp>(Expression<Func<T, TProp>> s)
    {
        QueryRequest newReq = _req with { OrderBy = new Order(PythagorasQueryHelpers<T>.GetApiName(s), false) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> Skip(int count)
    {
        if (count < 0)
        {
            throw new ArgumentException("Skip must be >= 0.", nameof(count));
        }

        if (_usedPage)
        {
            throw new InvalidOperationException("Cannot use Skip() after Page(). Choose either Skip()/Take() or Page().");
        }

        int? mr = _req.Page?.MaxResults;
        QueryRequest newReq = _req with { Page = new Paging(count, mr) };
        return new PythagorasQuery<T>(newReq, usedSkip: true, _usedTake, _usedPage);
    }

    public PythagorasQuery<T> Page(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentException("Page must be 1+.", nameof(pageNumber));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentException("Page size must be > 0.", nameof(pageSize));
        }

        if (_usedSkip || _usedTake)
        {
            throw new InvalidOperationException("Cannot use Page() after Skip()/Take(). Choose either Page() or Skip()/Take(), not both.");
        }

        int firstResult = (pageNumber - 1) * pageSize;
        QueryRequest newReq = _req with { Page = new Paging(firstResult, pageSize) };
        return new PythagorasQuery<T>(newReq, _usedSkip, _usedTake, usedPage: true);
    }

    public PythagorasQuery<T> Take(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Take must be > 0.", nameof(count));
        }

        if (_usedPage)
        {
            throw new InvalidOperationException("Cannot use Take() after Page(). Choose either Page() or Skip()/Take(), not both.");
        }

        int? fr = _req.Page?.FirstResult;
        QueryRequest newReq = _req with { Page = new Paging(fr, count) };
        return new PythagorasQuery<T>(newReq, _usedSkip, usedTake: true, _usedPage);
    }

    public string BuildAsQueryString() => QueryStringWriter.Build(_req);

    public HttpRequestMessage Build(HttpMethod method, string baseUrl)
    {
        string queryString = BuildAsQueryString();

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return new HttpRequestMessage(method, baseUrl);
        }

        UriBuilder uriBuilder = new(baseUrl);

        if (string.IsNullOrEmpty(uriBuilder.Query))
        {
            uriBuilder.Query = queryString;
        }
        else
        {
            string existingQuery = uriBuilder.Query.TrimStart('?');
            uriBuilder.Query = $"{existingQuery}&{queryString}";
        }

        return new HttpRequestMessage(method, uriBuilder.Uri);
    }
}

public sealed record Filter(FieldTarget Target, string Field, Op Operator, string Value);
public sealed record Order(string Field, bool Asc = true);
public sealed record Paging(int? FirstResult, int? MaxResults);
