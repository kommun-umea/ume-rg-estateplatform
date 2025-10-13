using System.Text.Json;
using Umea.se.EstateService.Logic.Search.Analysis;
using Umea.se.EstateService.Logic.Search.Indexing;
using Umea.se.EstateService.Shared.Search;

namespace Umea.se.EstateService.Logic.Search;

public sealed class InMemorySearchService
{
    private readonly List<PythagorasDocument> _docs = [];
    private readonly TermIndex _idx = new();
    private readonly Dictionary<int, int> _docLengths = []; // token count per doc
    private readonly Dictionary<string, int> _termFrequencies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _idf = new(StringComparer.Ordinal);

    public int DocumentCount => _docs.Count;

    private readonly int _symSpellMaxEdits = 2;
    private SymSpell _symSpell = new(1024, 2);

    // Field weights (tweak to taste)
    private readonly Dictionary<Field, double> _w = new()
    {
        { Field.Name, 20.0 },
        { Field.PopularName, 25.0 },
        { Field.Path, 2.0 },
        { Field.AncestorName, 1.5 },
        { Field.AncestorPopularName, 1.0 },
    };

    // strong bump when the term hits the start of key fields
    private readonly double _startsWithBonus = 50.0;

    // bonus for full-field matches to keep exact hits ahead of longer prefixes
    private readonly double _exactMatchBonus = 60.0;

    // extra reward when the popular name starts with the query
    private readonly double _popularStartsWithBonus = 12.0;

    // prefer broader results by default unless overridden by field matches
    private readonly Dictionary<NodeType, double> _typeBaseBoost = new()
    {
        { NodeType.Estate, 15.0 },
        { NodeType.Building, 0.0 },
        { NodeType.Room, 0.0 }
    };

    public InMemorySearchService(IEnumerable<PythagorasDocument> items)
    {
        _symSpell = new SymSpell(1024, _symSpellMaxEdits);
        Build(items);
    }

    public static InMemorySearchService FromJsonFile(string path)
    {
        string json = File.ReadAllText(path);
        List<PythagorasDocument> items = JsonSerializer.Deserialize<List<PythagorasDocument>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
        return new InMemorySearchService(items);
    }

    private void Build(IEnumerable<PythagorasDocument> items)
    {
        _docs.Clear();
        _idx.Inverted.Clear();
        _idx.Prefixes.Clear();
        _docLengths.Clear();
        _termFrequencies.Clear();
        _idf.Clear();

        int docId = 0;
        foreach (PythagorasDocument it in items)
        {
            _docs.Add(it);
            int tokenCount = 0;

            void IndexField(Field f, string? value)
            {
                int pos = 0;
                foreach (string tok in TextNormalizer.Tokenize(value))
                {
                    _idx.Add(tok, docId, f, pos++);
                    tokenCount++;
                    if (_termFrequencies.TryGetValue(tok, out int count))
                    {
                        _termFrequencies[tok] = count + 1;
                    }
                    else
                    {
                        _termFrequencies[tok] = 1;
                    }
                }
            }

            IndexField(Field.Name, it.Name);
            IndexField(Field.PopularName, it.PopularName);
            IndexField(Field.Path, it.Path);
            foreach (Ancestor a in it.Ancestors ?? [])
            {
                IndexField(Field.AncestorName, a.Name);
                IndexField(Field.AncestorPopularName, a.PopularName);
            }

            _docLengths[docId] = Math.Max(tokenCount, 1);
            docId++;
        }

        int capacity = Math.Max(1024, _termFrequencies.Count);
        _symSpell = new SymSpell(capacity, _symSpellMaxEdits);
        foreach (KeyValuePair<string, int> kv in _termFrequencies)
        {
            _symSpell.CreateDictionaryEntry(kv.Key, kv.Value);
        }

        int docCount = _docs.Count;
        if (docCount > 0)
        {
            foreach (KeyValuePair<string, List<Posting>> entry in _idx.Inverted)
            {
                int df = entry.Value.Select(static p => p.DocId).Distinct().Count();
                _idf[entry.Key] = ComputeIdf(docCount, df);
            }
        }
    }

    private List<HashSet<string>> ExpandQueryTokens(string[] qTokens, QueryOptions options)
    {
        List<HashSet<string>> termCandidates = [];
        foreach (string? qt in qTokens)
        {
            HashSet<string> bucket = new(StringComparer.Ordinal);

            // exact
            if (_idx.Inverted.ContainsKey(qt))
            {
                bucket.Add(qt);
            }

            // prefix
            if (options.EnablePrefix)
            {
                string key = qt.Length >= 3 ? qt[..3] : qt;
                if (_idx.Prefixes.TryGetValue(key, out HashSet<string>? set))
                {
                    foreach (string t in set)
                    {
                        if (t.StartsWith(qt, StringComparison.Ordinal))
                        {
                            bucket.Add(t);
                        }
                    }
                }
            }

            // fuzzy (only for short-ish tokens)
            if (options.EnableFuzzy && qt.Length >= 3)
            {
                int maxEdits = Math.Min(options.FuzzyMaxEdits, _symSpellMaxEdits);
                if (maxEdits > 0 && _termFrequencies.Count > 0)
                {
                    foreach (SymSpell.SuggestItem? suggestion in _symSpell.Lookup(qt, SymSpell.Verbosity.Closest, maxEdits))
                    {
                        bucket.Add(suggestion.term);
                    }
                }
            }

            // at least keep the original token for later reporting
            if (bucket.Count == 0 && _idx.Inverted.ContainsKey(qt) == false)
            {
                // no candidates – keep as-is to avoid empty intersections
                bucket.Add(qt);
            }
            termCandidates.Add(bucket);
        }
        return termCandidates;
    }

    private HashSet<int> FindCandidateDocuments(List<HashSet<string>> termCandidates)
    {
        HashSet<int>? finalDocIds = null;

        foreach (HashSet<string> bucket in termCandidates)
        {
            HashSet<int> docIdsForTerm = [];
            foreach (string term in bucket)
            {
                if (_idx.Inverted.TryGetValue(term, out List<Posting>? postings))
                {
                    foreach (Posting p in postings)
                    {
                        docIdsForTerm.Add(p.DocId);
                    }
                }
            }

            if (finalDocIds == null)
            {
                finalDocIds = docIdsForTerm;
            }
            else
            {
                finalDocIds.IntersectWith(docIdsForTerm);
            }

            // short-circuit if no docs match all terms so far
            if (finalDocIds.Count == 0)
            {
                return [];
            }
        }

        return finalDocIds ?? [];
    }

    private (Dictionary<int, double>, Dictionary<int, Dictionary<string, string>>) ScoreDocuments(
        List<HashSet<string>> termCandidates,
        HashSet<int> candidateDocIds,
        string[] qTokens,
        string normalizedQuery)
    {
        double avgdl = _docLengths.Count > 0 ? _docLengths.Values.Average() : 1.0;

        Dictionary<int, double> docScores = [];
        Dictionary<int, Dictionary<string, string>> matchedPerDoc = []; // queryToken -> matchedIndexTerm
        HashSet<int> exactMatchAwarded = [];

        for (int qi = 0; qi < termCandidates.Count; qi++)
        {
            string queryToken = qTokens[qi];
            HashSet<string> candidates = termCandidates[qi];
            HashSet<int> seenDocs = [];

            foreach (string term in candidates)
            {
                if (!_idx.Inverted.TryGetValue(term, out List<Posting>? postings) ||
                    !_idf.TryGetValue(term, out double idf))
                {
                    continue;
                }

                foreach (IGrouping<int, Posting> group in postings.GroupBy(static p => p.DocId))
                {
                    int docId = group.Key;
                    if (!candidateDocIds.Contains(docId))
                    {
                        continue;
                    }

                    seenDocs.Add(docId);

                    double tfWeighted = 0.0;
                    double startsWithBonus = 0.0;
                    bool popularStartsWith = false;

                    foreach (Posting p in group)
                    {
                        bool isPrefixHit = p.Positions.Contains(0);
                        bool isRealPrefix = term.StartsWith(queryToken, StringComparison.Ordinal);

                        if (startsWithBonus == 0.0 &&
                            (p.Field == Field.Name || p.Field == Field.PopularName) &&
                            isPrefixHit &&
                            isRealPrefix)
                        {
                            startsWithBonus = _startsWithBonus;
                        }

                        if (p.Field == Field.PopularName && isPrefixHit && isRealPrefix)
                        {
                            popularStartsWith = true;
                        }

                        tfWeighted += _w[p.Field] * p.Positions.Count;
                    }

                    double bm25 = ComputeBm25(tfWeighted, idf, _docLengths[docId], avgdl);
                    double proximityBonus = 0.05 * group.Count();

                    double score = docScores.TryGetValue(docId, out double current)
                        ? current
                        : _typeBaseBoost.TryGetValue(_docs[docId].Type, out double boost) ? boost : 0.0;

                    if (normalizedQuery.Length > 0 && !exactMatchAwarded.Contains(docId))
                    {
                        string normalizedName = TextNormalizer.Normalize(_docs[docId].Name).Trim();
                        string normalizedPopular = TextNormalizer.Normalize(_docs[docId].PopularName ?? string.Empty).Trim();
                        if (string.Equals(normalizedName, normalizedQuery, StringComparison.Ordinal) ||
                            string.Equals(normalizedPopular, normalizedQuery, StringComparison.Ordinal))
                        {
                            score += _exactMatchBonus;
                            exactMatchAwarded.Add(docId);
                        }
                    }

                    if (startsWithBonus > 0)
                    {
                        score += startsWithBonus;
                    }

                    if (popularStartsWith)
                    {
                        score += _popularStartsWithBonus;
                    }

                    score += bm25 + proximityBonus;
                    docScores[docId] = score;

                    if (!matchedPerDoc.TryGetValue(docId, out Dictionary<string, string>? map))
                    {
                        matchedPerDoc[docId] = map = [];
                    }

                    map[queryToken] = term;
                }
            }

            foreach (int docId in seenDocs)
            {
                if (docScores.TryGetValue(docId, out double current))
                {
                    docScores[docId] = current + 0.2;
                }
            }
        }

        return (docScores, matchedPerDoc);
    }

    private void ApplyProximityBonus(
        Dictionary<int, double> docScores,
        Dictionary<int, Dictionary<string, string>> matchedPerDoc)
    {
        foreach (KeyValuePair<int, Dictionary<string, string>> kv in matchedPerDoc)
        {
            int docId = kv.Key;
            string[] indexTerms = [.. kv.Value.Values.Distinct()];
            if (indexTerms.Length < 2)
            {
                continue;
            }

            // collect all positions across fields (we just need any positions for the same doc)
            List<List<int>> posLists = [];
            foreach (string? t in indexTerms)
            {
                List<Posting> postings = _idx.Inverted[t];
                List<int> pos = [.. postings.Where(p => p.DocId == docId).SelectMany(p => p.Positions).OrderBy(x => x)];
                if (pos.Count > 0)
                {
                    posLists.Add(pos);
                }
            }
            if (posLists.Count < 2)
            {
                continue;
            }

            int bestSpan = CalculateMinimalSpan(posLists);
            if (bestSpan != int.MaxValue)
            {
                double increment = 1.0 / (1.0 + bestSpan);
                docScores[docId] += increment; // closer = bigger boost
            }
        }
    }

    private static int CalculateMinimalSpan(List<List<int>> posLists)
    {
        // compute minimal window covering one position from each list (classic k-way merge)
        int[] pointers = new int[posLists.Count];
        int bestSpan = int.MaxValue;
        while (true)
        {
            int minVal = int.MaxValue, maxVal = int.MinValue, minIdx = -1;
            for (int i = 0; i < posLists.Count; i++)
            {
                List<int> list = posLists[i];
                int ptr = pointers[i];
                if (ptr >= list.Count) { minIdx = -1; break; }
                int v = list[ptr];
                if (v < minVal) { minVal = v; minIdx = i; }
                if (v > maxVal)
                {
                    maxVal = v;
                }
            }
            if (minIdx == -1)
            {
                break;
            }

            int span = Math.Max(0, maxVal - minVal);
            if (span < bestSpan)
            {
                bestSpan = span;
            }

            pointers[minIdx]++;
        }
        return bestSpan;
    }

    private static double ComputeIdf(int totalDocs, int documentFrequency)
    {
        return Math.Log(1 + (totalDocs - documentFrequency + 0.5) / (documentFrequency + 0.5));
    }

    private static double ComputeBm25(double tfWeighted, double idf, int docLength, double avgdl)
    {
        if (tfWeighted <= 0 || idf <= 0)
        {
            return 0;
        }

        const double k1 = 1.2;
        const double b = 0.75;
        double denominator = tfWeighted + k1 * (1 - b + b * (docLength / avgdl));
        return denominator <= 0 ? 0 : idf * (tfWeighted * (k1 + 1)) / denominator;
    }

    private IEnumerable<SearchResult> ComposeResults(
        Dictionary<int, double> docScores,
        Dictionary<int, Dictionary<string, string>> matchedPerDoc,
        QueryOptions options)
    {
        IEnumerable<KeyValuePair<int, double>> sortedDocs = docScores
            .OrderByDescending(kv2 => kv2.Value)
            .ThenBy(kv2 => options.PreferEstatesOnTie ? (_docs[kv2.Key].Type == NodeType.Estate ? 0 : 1) : 0)
            .ThenBy(kv2 => _docs[kv2.Key].PopularName ?? _docs[kv2.Key].Name);

        // Apply type filter before taking MaxResults
        if (options.FilterByType.HasValue)
        {
            sortedDocs = sortedDocs.Where(kv2 => _docs[kv2.Key].Type == options.FilterByType.Value);
        }

        SearchResult[] results = [.. sortedDocs
            .Take(options.MaxResults)
            .Select(kv2 => new SearchResult(
                _docs[kv2.Key],
                kv2.Value,
                matchedPerDoc.TryGetValue(kv2.Key, out Dictionary<string, string>? m) ? m : []))];

        return results;
    }

    public IEnumerable<SearchResult> Search(string query, QueryOptions? options = null)
    {
        options ??= new QueryOptions();
        string normalizedQuery = TextNormalizer.Normalize(query ?? string.Empty).Trim();
        string[] qTokens = [.. TextNormalizer.Tokenize(query)];

        if (qTokens.Length == 0)
        {
            return [];
        }

        List<HashSet<string>> termCandidates = ExpandQueryTokens(qTokens, options);

        HashSet<int> candidateDocIds = FindCandidateDocuments(termCandidates);
        if (candidateDocIds.Count == 0)
        {
            return [];
        }

        (Dictionary<int, double> docScores, Dictionary<int, Dictionary<string, string>> matchedPerDoc) =
            ScoreDocuments(termCandidates, candidateDocIds, qTokens, normalizedQuery);

        ApplyProximityBonus(docScores, matchedPerDoc);

        return ComposeResults(docScores, matchedPerDoc, options);
    }
}
