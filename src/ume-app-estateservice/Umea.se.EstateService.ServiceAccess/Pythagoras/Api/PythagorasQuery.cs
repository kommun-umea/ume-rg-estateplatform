namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

public enum FieldTarget { Parameter, Attribute }

public enum Op
{
    Eq, Ne, Gt, Ge, Lt, Le,
    LikeExact, LikeAnywhere, LikeStarts, LikeEnds,
    ILikeExact, ILikeAnywhere, ILikeStarts, ILikeEnds
}

public class PythagorasQuery<T> where T : class
{
    private readonly QueryRequest _req;
    private static readonly ConcurrentDictionary<MemberInfo, string> _nameCache = new();
    private bool _usedSkip;
    private bool _usedTake;
    private bool _usedPage;

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
        if (ids is { Length: > 0 })
        {
            _req.Ids.AddRange(ids);
        }

        return this;
    }

    public PythagorasQuery<T> GeneralSearch(string value)
    {
        _req.GeneralSearch = value;
        return this;
    }

    public PythagorasQuery<T> WithQueryParameter<TValue>(string name, TValue value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            throw new ArgumentException("Parameter name must contain characters.", nameof(name));
        }

        string formattedValue = FormatValue(value);
        _req.AdditionalParameters[trimmedName] = formattedValue;
        return this;
    }

    public PythagorasQuery<T> Where<TProp>(
        Expression<Func<T, TProp>> selector,
        Op op,
        TProp value,
        FieldTarget target = FieldTarget.Parameter)
    {
        string name = GetApiName(selector);
        string str = FormatValue(value);
        _req.Filters.Add(new Filter(target, name, op, str));
        return this;
    }

    public PythagorasQuery<T> Where<TProp>(
        Expression<Func<T, TProp>> selector,
        TProp value,
        FieldTarget target = FieldTarget.Parameter)
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

    public PythagorasQuery<T> Between<TProp>(Expression<Func<T, TProp>> s, TProp min, TProp max)
        => GreaterThanOrEqual(s, min).LessThanOrEqual(s, max);

    public PythagorasQuery<T> WhereAttribute<TProp>(Expression<Func<T, TProp>> s, string op, TProp v)
        => Where(s, OperatorMaps.FromStringOrPrefix(op), v, FieldTarget.Attribute);

    public PythagorasQuery<T> WhereParameter<TProp>(Expression<Func<T, TProp>> s, string op, TProp v)
        => Where(s, OperatorMaps.FromStringOrPrefix(op), v, FieldTarget.Parameter);

    public PythagorasQuery<T> OrderBy<TProp>(Expression<Func<T, TProp>> s)
    {
        _req.OrderBy = new Order(GetApiName(s), true);
        return this;
    }

    public PythagorasQuery<T> OrderByDescending<TProp>(Expression<Func<T, TProp>> s)
    {
        _req.OrderBy = new Order(GetApiName(s), false);
        return this;
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

        _usedSkip = true;
        int? mr = _req.Page?.MaxResults;
        _req.Page = new Paging(count, mr);
        return this;
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
        _req.Page = new Paging(firstResult, pageSize);
        _usedPage = true;
        return this;
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
        _req.Page = new Paging(fr, count);
        _usedTake = true;
        return this;
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

    public PythagorasQuery<T> Clone()
        => new(_req.Clone(), _usedSkip, _usedTake, _usedPage);

    // ---- Helpers ----

    private static string GetApiName<TProp>(Expression<Func<T, TProp>> expr)
    {
        PropertyInfo mi = expr.Body switch
        {
            MemberExpression m when m.Member is PropertyInfo pi => pi,
            UnaryExpression u when u.Operand is MemberExpression um && um.Member is PropertyInfo upi => upi,
            _ => throw new ArgumentException($"Expression must be a property access on {typeof(T).Name}.", nameof(expr))
        };

        return _nameCache.GetOrAdd(mi, static m =>
        {
            JsonPropertyNameAttribute? json = m.GetCustomAttribute<JsonPropertyNameAttribute>();
            return json?.Name ?? ToCamelCase(m.Name);
        });
    }

    private static string ToCamelCase(string s)
        => string.IsNullOrEmpty(s) || char.IsLower(s[0]) ? s : char.ToLowerInvariant(s[0]) + s[1..];

    private static string FormatValue<TProp>(TProp value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime dt => dt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture)
                : dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}

internal sealed record Filter(FieldTarget Target, string Field, Op Operator, string Value);
internal sealed record Order(string Field, bool Asc = true);
internal sealed record Paging(int? FirstResult, int? MaxResults);

internal sealed class QueryRequest
{
    public List<int> Ids { get; } = [];
    public string? GeneralSearch { get; set; }
    public List<Filter> Filters { get; } = [];
    public Order? OrderBy { get; set; }
    public Paging? Page { get; set; }

    public Dictionary<string, string> AdditionalParameters { get; } = new(StringComparer.OrdinalIgnoreCase);

    public QueryRequest Clone()
    {
        QueryRequest copy = new()
        {
            GeneralSearch = GeneralSearch,
            OrderBy = OrderBy,
            Page = Page
        };

        copy.Ids.AddRange(Ids);
        copy.Filters.AddRange(Filters);
        foreach ((string key, string value) in AdditionalParameters)
        {
            copy.AdditionalParameters[key] = value;
        }

        return copy;
    }
}

public static class OperatorMaps
{
    private static readonly Dictionary<Op, string> _prefix = new()
    {
        [Op.Eq] = "EQ:",
        [Op.Ne] = "NE:",
        [Op.Gt] = "GT:",
        [Op.Ge] = "GE:",
        [Op.Lt] = "LT:",
        [Op.Le] = "LE:",
        [Op.LikeExact] = "LIKEEX:",
        [Op.LikeAnywhere] = "LIKEAW:",
        [Op.LikeStarts] = "LIKEST:",
        [Op.LikeEnds] = "LIKEEN:",
        [Op.ILikeExact] = "ILIKEEX:",
        [Op.ILikeAnywhere] = "ILIKEAW:",
        [Op.ILikeStarts] = "ILIKEST:",
        [Op.ILikeEnds] = "ILIKEEN:",
    };

    public static string ToPrefix(Op op) => _prefix[op];

    public static Op FromStringOrPrefix(string op)
    {
        string u = op.Trim().ToUpperInvariant().TrimEnd(':');
        if (u.Length == 0)
        {
            throw new ArgumentException("Operator cannot be empty.", nameof(op));
        }
        return u switch
        {
            "EQ" or "==" or "=" => Op.Eq,
            "NE" or "!=" => Op.Ne,
            "GT" or ">" => Op.Gt,
            "GE" or ">=" => Op.Ge,
            "LT" or "<" => Op.Lt,
            "LE" or "<=" => Op.Le,
            "LIKEEX" => Op.LikeExact,
            "LIKEAW" => Op.LikeAnywhere,
            "LIKEST" => Op.LikeStarts,
            "LIKEEN" => Op.LikeEnds,
            "ILIKEEX" => Op.ILikeExact,
            "ILIKEAW" => Op.ILikeAnywhere,
            "ILIKEST" => Op.ILikeStarts,
            "ILIKEEN" => Op.ILikeEnds,
            _ => throw new ArgumentException($"Unknown or unsupported operator '{u}'.", nameof(op))
        };
    }
}

internal static class QueryStringWriter
{
    private static readonly HashSet<Op> _likeOps =
    [
        Op.LikeExact,
        Op.LikeAnywhere,
        Op.LikeStarts,
        Op.LikeEnds,
        Op.ILikeExact,
        Op.ILikeAnywhere,
        Op.ILikeStarts,
        Op.ILikeEnds
    ];

    private static readonly HashSet<Op> _comparisonOps =
    [
        Op.Gt,
        Op.Ge,
        Op.Lt,
        Op.Le
    ];

    internal static string Build(QueryRequest req)
    {
        Validate(req);

        List<KeyValuePair<string, string>> parts = [];

        foreach (int id in req.Ids)
        {
            parts.Add(new("id[]", id.ToString(CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(req.GeneralSearch))
        {
            parts.Add(new("generalSearch", req.GeneralSearch!));
        }

        foreach (Filter f in req.Filters)
        {
            string prefixedName = $"{OperatorMaps.ToPrefix(f.Operator)}{f.Field}";
            if (f.Target == FieldTarget.Parameter)
            {
                parts.Add(new("pN[]", prefixedName));
                parts.Add(new("pV[]", f.Value));
            }
            else
            {
                parts.Add(new("aN[]", prefixedName));
                parts.Add(new("aV[]", f.Value));
            }
        }

        if (req.OrderBy is not null)
        {
            parts.Add(new("orderBy", req.OrderBy.Field));
            parts.Add(new("orderAsc", req.OrderBy.Asc ? "true" : "false"));
        }

        if (req.Page is not null)
        {
            if (req.Page.FirstResult is int fr)
            {
                parts.Add(new("firstResult", fr.ToString(CultureInfo.InvariantCulture)));
            }

            if (req.Page.MaxResults is int mr)
            {
                parts.Add(new("maxResults", mr.ToString(CultureInfo.InvariantCulture)));
            }
        }

        foreach ((string key, string value) in req.AdditionalParameters)
        {
            parts.Add(new(key, value));
        }

        return Encode(parts);
    }

    private static void Validate(QueryRequest req)
    {
        if (req.Ids.Count > 0 && !string.IsNullOrWhiteSpace(req.GeneralSearch))
        {
            throw new InvalidOperationException("Cannot combine WithIds() and GeneralSearch().");
        }

        if (req.Page is { MaxResults: int mr } && mr <= 0)
        {
            throw new ArgumentException("Take must be > 0.");
        }

        if (req.Page is { FirstResult: int fr } && fr < 0)
        {
            throw new ArgumentException("Skip must be >= 0.");
        }

        // Enforce Page() semantics - Page() without Take() is a no-op and should fail
        // Detect conflicts only within same field & target
        IEnumerable<IGrouping<(FieldTarget Target, string Field), Filter>> groups = req.Filters
            .GroupBy(f => (f.Target, f.Field))
            .Where(g => g.Count() > 1);

        foreach (IGrouping<(FieldTarget Target, string Field), Filter>? g in groups)
        {
            List<Op> ops = [.. g.Select(x => x.Operator)];
            if (HasConflicts(ops))
            {
                throw new InvalidOperationException($"Conflicting filters for '{g.Key.Target}:{g.Key.Field}': {string.Join(", ", ops)}");
            }
        }
    }

    private static bool HasConflicts(List<Op> ops)
    {
        // No duplicates allowed
        if (ops.Count != ops.Distinct().Count())
        {
            return true;
        }

        HashSet<Op> opsSet = [.. ops];

        // Eq is mutually exclusive with any other operator
        if (opsSet.Contains(Op.Eq) && opsSet.Count > 1)
        {
            return true;
        }

        // Ne is mutually exclusive with exact likes
        if (opsSet.Contains(Op.Ne) && opsSet.Overlaps([Op.LikeExact, Op.ILikeExact]))
        {
            return true;
        }

        // Cannot mix Like and comparison operators
        bool anyLike = opsSet.Overlaps(_likeOps);
        bool anyCmp = opsSet.Overlaps(_comparisonOps);
        if (anyLike && anyCmp)
        {
            return true;
        }

        // Allow at most one "greater than" type and one "less than" type
        int gtCount = opsSet.Count(o => o is Op.Gt or Op.Ge);
        int ltCount = opsSet.Count(o => o is Op.Lt or Op.Le);

        if (gtCount > 1 || ltCount > 1)
        {
            return true;
        }

        return false;
    }

    private static string Encode(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        static string E(string s) => Uri.EscapeDataString(s);
        StringBuilder sb = new();
        foreach ((string k, string v) in pairs)
        {
            if (sb.Length > 0)
            {
                sb.Append('&');
            }

            sb.Append(E(k)).Append('=').Append(E(v));
        }
        return sb.ToString();
    }
}
