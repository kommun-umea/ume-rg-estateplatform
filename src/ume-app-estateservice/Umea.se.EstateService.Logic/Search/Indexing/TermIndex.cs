namespace Umea.se.EstateService.Logic.Search.Indexing;

internal sealed class TermIndex
{
    // normalized term -> postings
    public Dictionary<string, List<Posting>> Inverted { get; } = new(StringComparer.Ordinal);
    // for prefix search: map first 1-3 chars -> set of terms
    public Dictionary<string, HashSet<string>> Prefixes { get; } = new(StringComparer.Ordinal);

    public void Add(string term, int docId, Field field, int position)
    {
        if (!Inverted.TryGetValue(term, out List<Posting>? list))
        {
            list = new List<Posting>();
            Inverted[term] = list;
        }
        Posting? last = list.Count > 0 ? list[^1] : null;
        if (last != null && last.DocId == docId && last.Field == field)
        {
            last.Positions.Add(position);
        }
        else
        {
            list.Add(new Posting(docId, field, position));
        }

        // register prefixes up to length 3 for compactness
        for (int len = 1; len <= Math.Min(3, term.Length); len++)
        {
            string p = term.AsSpan(0, len).ToString();
            if (!Prefixes.TryGetValue(p, out HashSet<string>? set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                Prefixes[p] = set;
            }
            set.Add(term);
        }
    }
}
