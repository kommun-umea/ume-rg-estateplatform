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

    public int DocumentCount => _docs.Count;

    private readonly int _symSpellMaxEdits = 2;
    private SymSpell _symSpell = new(1024, 2);

    // Field weights (tweak to taste)
    private readonly Dictionary<Field, double> _w = new()
    {
        { Field.Name, 20.0 },
        { Field.PopularName, 15.0 },
        { Field.Path, 2.0 },
        { Field.AncestorName, 1.5 },
        { Field.AncestorPopularName, 1.0 },
    };

    // strong bump when the term hits the start of key fields
    private readonly double _startsWithBonus = 50.0;

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
    }

    public IEnumerable<SearchResult> Search(string query, QueryOptions? options = null)
    {
        options ??= new QueryOptions();
        string[] qTokens = [.. TextNormalizer.Tokenize(query)];

        if (qTokens.Length == 0)
        {
            return [];
        }

        // Expand each token to a set of candidate index terms (exact, prefix, fuzzy)
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

        // =================================================================================
        // 1. Find candidate documents (AND logic)
        // =================================================================================
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

        if (finalDocIds == null || finalDocIds.Count == 0)
        {
            return [];
        }

        int N = _docs.Count;
        if (_docs.Count == 0)
        {
            return [];
        }

        double avgdl = _docLengths.Count > 0 ? _docLengths.Values.Average() : 1.0;

        // =================================================================================
        // 2. Score documents that matched ALL terms
        // =================================================================================
        Dictionary<int, double> docScores = [];
        Dictionary<int, Dictionary<string, string>> matchedPerDoc = []; // queryToken -> matchedIndexTerm

        for (int qi = 0; qi < termCandidates.Count; qi++)
        {
            HashSet<string> candidates = termCandidates[qi];
            HashSet<int> seenDocs = [];
            foreach (string term in candidates)
            {
                if (!_idx.Inverted.TryGetValue(term, out List<Posting>? postings))
                {
                    continue;
                }

                // document frequency for IDF
                int df = postings.Select(p => p.DocId).Distinct().Count();
                double idf = Math.Log(1 + (N - df + 0.5) / (df + 0.5)); // BM25-ish idf

                // aggregate per doc across fields
                IEnumerable<IGrouping<int, Posting>> byDoc = postings.GroupBy(p => p.DocId);
                foreach (IGrouping<int, Posting> group in byDoc)
                {
                    int docId = group.Key;

                    // --- THIS IS THE CORE CHANGE ---
                    // Only score docs that are in our final candidate set
                    if (!finalDocIds.Contains(docId))
                    {
                        continue;
                    }
                    // -----------------------------

                    seenDocs.Add(docId);

                    // field-weighted term frequency
                    double tfWeighted = 0.0;
                    double startsWithBonus = 0.0;
                    Dictionary<Field, List<int>> fieldPositions = [];
                    foreach (Posting? p in group)
                    {
                        if (startsWithBonus == 0.0 &&
                            (p.Field == Field.Name || p.Field == Field.PopularName) &&
                            p.Positions.Contains(0))
                        {
                            startsWithBonus = _startsWithBonus;
                        }

                        tfWeighted += _w[p.Field] * p.Positions.Count;
                        fieldPositions[p.Field] = p.Positions;
                    }

                    // BM25-lite with field weights folded into tf
                    int dl = _docLengths[docId];
                    const double k1 = 1.2, b = 0.75;
                    double bm25 = idf * (tfWeighted * (k1 + 1)) / (tfWeighted + k1 * (1 - b + b * (dl / avgdl)));

                    // proximity bonus: if multiple query tokens later map into same doc, we'll compute a minimal span
                    // here we add a small self-bonus that gets amplified when spans overlap in the final pass
                    double proximityBonus = 0.05 * group.Count();

                    if (!docScores.TryGetValue(docId, out double s))
                    {
                        s = 0;
                    }

                    s += bm25 + proximityBonus + startsWithBonus;
                    docScores[docId] = s;

                    // record match representative
                    if (!matchedPerDoc.TryGetValue(docId, out Dictionary<string, string>? map))
                    {
                        matchedPerDoc[docId] = map = [];
                    }

                    map[qTokens[qi]] = term;
                }
            }

            // coverage bonus: docs that matched this query term get a small boost
            foreach (int docId in seenDocs)
            {
                if (docScores.TryGetValue(docId, out double value)) // check existence as it might have been filtered
                {
                    docScores[docId] = value + 0.2;
                }
            }
        }

        // =================================================================================
        // 3. Proximity bonus (only for docs that already passed the AND filter)
        // =================================================================================
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
            if (bestSpan != int.MaxValue)
            {
                docScores[docId] += 1.0 / (1.0 + bestSpan); // closer = bigger boost
            }
        }

        // Compose results
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
            .Select(kv2 => new SearchResult(_docs[kv2.Key], kv2.Value, matchedPerDoc.TryGetValue(kv2.Key, out Dictionary<string, string>? m) ? m : []))];

        return results;
    }
}
