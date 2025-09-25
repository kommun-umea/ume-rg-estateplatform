namespace Umea.se.EstateService.ServiceAccess.Pythagoras.Api.Query;

using System.Globalization;
using System.Text;

internal static class QueryStringWriter
{
    private static readonly HashSet<string> _reservedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "id[]", "generalSearch", "pN[]", "pV[]", "aN[]", "aV[]",
        "orderBy", "orderAsc", "firstResult", "maxResults"
    };

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

    internal static bool IsReservedKey(string key) => _reservedKeys.Contains(key);

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
        if (ops.Count != ops.Distinct().Count())
        {
            return true;
        }

        HashSet<Op> opsSet = [.. ops];

        if (opsSet.Contains(Op.Eq) && opsSet.Count > 1)
        {
            return true;
        }

        if (opsSet.Contains(Op.Ne) && opsSet.Overlaps([Op.LikeExact, Op.ILikeExact]))
        {
            return true;
        }

        bool anyLike = opsSet.Overlaps(_likeOps);
        bool anyCmp = opsSet.Overlaps(_comparisonOps);
        if (anyLike && anyCmp)
        {
            return true;
        }

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