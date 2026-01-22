# Search System Documentation

**Date:** 2026-01-22
**File:** `InMemorySearchService.cs`

This document describes the in-memory search system used for searching estates, buildings, and rooms in the Estate Platform.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Indexing](#indexing)
4. [Query Processing](#query-processing)
5. [Scoring Algorithm](#scoring-algorithm)
6. [Geospatial Filtering](#geospatial-filtering)
7. [Configuration Constants](#configuration-constants)
8. [Diagnostics API](#diagnostics-api)

---

## Overview

The search system is a custom in-memory full-text search engine optimized for searching Swedish property data. It supports:

- **Exact matching** - Direct term lookups
- **Prefix matching** - "stads" matches "stadshuset"
- **N-gram matching** - Substring search via character n-grams
- **Fuzzy matching** - Typo tolerance using SymSpell algorithm
- **Geospatial filtering** - Radius and bounding box filters
- **Field-weighted scoring** - Different importance for Name, PopularName, Address, etc.

---

## Architecture

### Data Structures

```
InMemorySearchService
├── _docs: List<PythagorasDocument>     # All indexed documents
├── _idx: TermIndex                      # Inverted index + prefix index
│   ├── Inverted: Dict<term, List<Posting>>
│   └── Prefixes: Dict<prefix3, HashSet<terms>>
├── _docLengths: Dict<docId, tokenCount> # For BM25 normalization
├── _termFrequencies: Dict<term, count>  # Global term counts
├── _idf: Dict<term, idf>                # Pre-computed IDF values
└── _symSpell: SymSpell                  # Fuzzy matching dictionary
```

### Document Model

Each `PythagorasDocument` contains:

| Field             | Description                      | Indexed?    | N-grams? |
|-------------------|----------------------------------|-------------|----------|
| Name              | Official name (e.g., "458-08")   | Yes         | Yes      |
| PopularName       | Common name (e.g., "Stadshuset") | Yes         | Yes      |
| Path              | Hierarchical path                | Yes         | No       |
| AddressSearchText | Street address                   | Yes         | No       |
| Ancestors         | Parent entities                  | Yes         | No       |
| Type              | Estate/Building/Room             | Filter only | -        |
| GeoLocation       | Lat/Lng coordinates              | Filter only | -        |

---

## Indexing

### Index Build Process

```
For each document:
  1. Index Name field (with n-grams)
  2. Index PopularName field (with n-grams)
  3. Index Path field
  4. Index Address field
  5. Index all Ancestor names
  6. Store document length (token count)
  7. Update term frequencies

Post-processing:
  1. Build SymSpell dictionary from term frequencies
  2. Compute IDF for all terms
```

### N-gram Generation

N-grams are generated for Name and PopularName fields to enable substring matching.

**Parameters:**
- Minimum n-gram size: 3
- Maximum n-gram size: 6

**Example:** Token "stadshuset" generates:
```
3-grams: sta, tad, ads, dsh, shu, hus, use, set
4-grams: stad, tads, adsh, dshu, shus, huse, uset
5-grams: stads, tadsh, adshu, dshus, shuse, huset
6-grams: stadsh, tadshu, adshus, dshuse, shuset
```

N-gram postings use position `-1` to distinguish them from regular term positions.

### Posting Structure

```csharp
record Posting(int DocId, Field Field, List<int> Positions);
```

- **DocId**: Internal document index
- **Field**: Which field the term appears in
- **Positions**: Token positions (0-indexed), or -1 for n-gram matches

---

## Query Processing

### Search Pipeline

```
Input: "skolgatan 31"
           │
           ▼
┌─────────────────────────┐
│  1. Normalization       │  → "skolgatan 31"
│     TextNormalizer      │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│  2. Tokenization        │  → ["skolgatan", "31"]
│     Split + lowercase   │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│  3. Token Expansion     │  → skolgatan: {skolgatan, skolgat...}
│     Exact/Prefix/Fuzzy  │     31: {31, 31c, 313...}
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│  4. Candidate Finding   │  → Docs matching ALL tokens
│     Set intersection    │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│  5. Scoring             │  → BM25 + bonuses per doc
│     Field weights       │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│  6. Geo Filtering       │  → Remove docs outside area
│     (optional)          │     + closeness boost
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│  7. Result Composition  │  → Sort by score, apply limits
│     Sort + filter       │
└─────────────────────────┘
           │
           ▼
Output: Ranked SearchResult[]
```

### Token Expansion

Each query token is expanded to find candidate index terms:

#### 1. Exact Match
```csharp
if (_idx.Inverted.ContainsKey(queryToken))
    bucket.Add(queryToken);
```

#### 2. Prefix Match (if enabled)
```csharp
// Look up 3-char prefix bucket, find terms starting with query token
prefix = queryToken[..3];
foreach (term in _idx.Prefixes[prefix])
    if (term.StartsWith(queryToken) && term != queryToken)
        bucket.Add(term);
```

#### 3. N-gram/Contains Match (if enabled)
```csharp
// Direct lookup - check if query token exists in index
// (either as full term or as n-gram of longer term)
if (_idx.Inverted.ContainsKey(queryToken))
    bucket.Add(queryToken);
```

**Note:** N-gram lookup is direct (not generating n-grams from query). This reduces noise from short substring matches.

#### 4. Fuzzy Match (if enabled)
```csharp
// SymSpell lookup for typo correction (max 2 edits)
foreach (suggestion in _symSpell.Lookup(queryToken, Closest, maxEdits))
    bucket.Add(suggestion.term);
```

### Candidate Document Finding

Documents must match ALL query tokens (AND semantics):

```csharp
foreach (tokenBucket in termCandidates)
{
    docsForToken = Union of all postings for terms in bucket
    finalDocs = finalDocs.Intersect(docsForToken)
}
```

---

## Scoring Algorithm

### Score Components

The final score for a document is:

```
FinalScore = TypeBoost
           + BM25Score
           + ExactMatchBonus
           + StartsWithBonus
           + PopularStartsWithBonus
           + ExactTokenMatchBonus
           + SameFieldMultiTokenBonus
           + NgramContribution
           + ProximityBonus
           + TermMatchBonus
           + GeoBonus
```

### Field Weights

| Field               | Weight | Purpose                        |
|---------------------|--------|--------------------------------|
| PopularName         | 25.0   | Highest - common search target |
| Name                | 20.0   | Official identifier            |
| Address             | 8.0    | Street address searches        |
| Path                | 2.0    | Hierarchical context           |
| AncestorName        | 1.5    | Parent entity names            |
| AncestorPopularName | 1.0    | Parent popular names           |

### Type Base Boost

| NodeType | Boost | Purpose                |
|----------|-------|------------------------|
| Estate   | 15.0  | Prefer broader results |
| Building | 0.0   | Neutral                |
| Room     | 0.0   | Neutral                |

### BM25 Scoring

The core relevance score uses BM25 (Best Match 25):

```csharp
// Parameters
k1 = 1.2  // Term frequency saturation
b = 0.75  // Document length normalization

// IDF (Inverse Document Frequency)
IDF(term) = log(1 + (N - df + 0.5) / (df + 0.5))
    where N = total docs, df = docs containing term

// TF (Term Frequency) - weighted by field and position
tfWeighted = Σ (fieldWeight × positionCount × positionWeight)

// BM25 formula
BM25 = IDF × (tfWeighted × (k1 + 1)) / (tfWeighted + k1 × (1 - b + b × (docLength / avgDocLength)))
```

### Position Weight

Terms appearing earlier in a field get higher weight:

```csharp
positionWeight = 1.0 / (1 + firstPosition)
// Position 0 → weight 1.0
// Position 1 → weight 0.5
// Position 2 → weight 0.33

// N-gram matches (position = -1) get reduced weight
if (position < 0)
    positionWeight = 0.6
```

### Bonus Scoring

#### Exact Match Bonus (+60)
When the **entire query** exactly matches a document's Name or PopularName:
```csharp
if (normalizedName == normalizedQuery || normalizedPopularName == normalizedQuery)
    score += 60.0;
```

#### Starts-With Bonus (+50)
When a term matches at position 0 in Name or PopularName:
```csharp
if ((field == Name || field == PopularName) && position == 0 && !isNumericToken)
    score += 50.0;
```
**Note:** Numeric tokens (e.g., "31") don't get this bonus to avoid boosting building numbers like "313-01" when searching for street number "31".

#### Popular Starts-With Bonus (+12)
Additional bonus when PopularName starts with the query term:
```csharp
if (field == PopularName && position == 0 && !isNumericToken)
    score += 12.0;
```

#### Exact Token Match Bonus (+25)
When a query token exactly matches an indexed term (not via prefix/fuzzy/n-gram expansion):
```csharp
if (term == queryToken)  // Exact string match
    score += 25.0;
```

#### Same-Field Multi-Token Bonus (+40 per extra token)
When multiple query tokens match in the same field:
```csharp
// "skolgatan 31" both matching in Address field
if (tokensInField >= 2)
    score += 40.0 × (tokensInField - 1);
```

**Important:** N-gram matches are excluded from this bonus (by design) to avoid amplifying noisy matches.

### N-gram Score Handling

N-gram matches are handled specially to reduce noise:

1. **Deduplication**: Only the best n-gram match per (document, queryToken, field) is counted
2. **IDF weighting**: Common n-grams (like "hem") contribute less
3. **Reduced multiplier**: N-gram scores are multiplied by 0.5

```csharp
ngramScore = bestContribution × 0.5 × IDF
```

### Proximity Bonus

When multiple terms appear close together in a document:

```csharp
// Find minimum span covering all matched terms
bestSpan = MinimalWindowSpan(termPositions)
proximityBonus = 1.0 / (1.0 + bestSpan)
```

Uses a k-way merge algorithm to find the smallest window containing one occurrence of each term.

### Term Match Increment (+0.2)

Small bonus for each query token that matches in a document:
```csharp
foreach (queryToken that matched)
    score += 0.2;
```

---

## Geospatial Filtering

### Radius Filter

Filter documents within a circular area:

```csharp
distance = Haversine(center, document.GeoLocation)
if (distance <= radiusMeters)
{
    closenessBoost = (radiusMeters - distance) / radiusMeters  // 0.0 to 1.0
    score += closenessBoost
}
```

### Bounding Box Filter

Filter documents within a rectangular area:

```csharp
if (lat >= south && lat <= north && lng >= west && lng <= east)
{
    // Boost documents closer to box center
    latDelta = |lat - centerLat| / latSpan
    lonDelta = |lng - centerLon| / lonSpan
    normalizedDistance = (latDelta + lonDelta) / 2
    closenessBoost = max(0, 1 - normalizedDistance)
    score += closenessBoost
}
```

---

## Configuration Constants

### Scoring Constants

| Constant                   | Value | Description                         |
|----------------------------|-------|-------------------------------------|
| `ExactMatchBonus`          | 60.0  | Full query matches Name/PopularName |
| `StartsWithBonus`          | 50.0  | Term at position 0 in key fields    |
| `SameFieldMultiTokenBonus` | 40.0  | Per extra token in same field       |
| `ExactTokenMatchBonus`     | 25.0  | Query token = index term            |
| `PopularStartsWithBonus`   | 12.0  | PopularName starts with term        |
| `NgramScoreMultiplier`     | 0.5   | Reduce n-gram contribution          |
| `NgramPositionWeight`      | 0.6   | Position weight for n-grams         |
| `TermMatchIncrement`       | 0.2   | Per-token match bonus               |
| `ProximityMultiplier`      | 0.05  | Base proximity factor               |

### BM25 Parameters

| Parameter | Value | Description               |
|-----------|-------|---------------------------|
| `k1`      | 1.2   | Term frequency saturation |
| `b`       | 0.75  | Length normalization      |

### Token Length Thresholds

| Threshold                | Value | Description                        |
|--------------------------|-------|------------------------------------|
| `MinPrefixLength`        | 3     | Min chars for prefix bucket lookup |
| `MinTokenLengthForFuzzy` | 3     | Min chars for fuzzy matching       |
| `MinTokenLengthForNgram` | 3     | Min chars for n-gram lookup        |

### Fuzzy Matching

| Parameter  | Value | Description                  |
|------------|-------|------------------------------|
| `MaxEdits` | 2     | Maximum Levenshtein distance |

---

## Diagnostics API

### Debug Endpoint

```
GET /search/debug?query=skolgatan+31
```

**Available in DEBUG builds only.**

### Response Structure

```json
{
  "results": [...],
  "diagnostics": {
    "originalQuery": "skolgatan 31",
    "normalizedQuery": "skolgatan 31",
    "queryTokens": ["skolgatan", "31"],
    "tokenExpansions": {
      "skolgatan": {
        "exactMatches": ["skolgatan"],
        "prefixMatches": [],
        "fuzzyMatches": [],
        "ngramMatches": [],
        "totalCandidates": 1
      }
    },
    "candidateDocumentCount": 42,
    "filteredDocumentCount": 42,
    "topScoreBreakdowns": [
      {
        "docId": 458,
        "name": "458-08",
        "popularName": "Stadshuset",
        "type": "Building",
        "typeBoost": 0.0,
        "bm25Score": 12.5,
        "exactMatchBonus": 0.0,
        "startsWithBonus": 50.0,
        "popularStartsWithBonus": 0.0,
        "proximityBonus": 0.5,
        "termMatchBonus": 0.4,
        "geoBonus": 0.0,
        "exactTokenMatchBonus": 50.0,
        "sameFieldMultiTokenBonus": 40.0,
        "ngramDedupeAdjustment": 0.0,
        "finalScore": 153.4,
        "fieldHits": [...],
        "matchedTerms": {
          "skolgatan": "skolgatan",
          "31": "31c"
        }
      }
    ],
    "appliedOptions": {
      "enablePrefix": true,
      "enableFuzzy": true,
      "enableContains": true,
      "fuzzyMaxEdits": 2,
      "maxResults": 10
    },
    "elapsedMilliseconds": 3
  }
}
```

---

## Query Options

| Option                  | Default | Description                               |
|-------------------------|---------|-------------------------------------------|
| `EnablePrefix`          | true    | Allow prefix matching                     |
| `EnableFuzzy`           | true    | Allow fuzzy (typo) matching               |
| `EnableContains`        | true    | Allow n-gram/substring matching           |
| `FuzzyMaxEdits`         | 2       | Maximum edit distance for fuzzy           |
| `MaxResults`            | 10      | Maximum results to return                 |
| `PreferEstatesOnTie`    | false   | Prefer estates when scores are equal      |
| `FilterByTypes`         | null    | Filter by NodeType (Estate/Building/Room) |
| `FilterByBusinessTypes` | null    | Filter by business type IDs               |
| `GeoFilter`             | null    | Radius or bounding box filter             |

---

## Example Score Calculation

**Query:** "skolgatan 31"
**Document:** Stadshuset (Building at Skolgatan 31C)

```
Base Score:
  TypeBoost (Building)           =   0.0

Token "skolgatan":
  Field: Address, Position: 0
  FieldWeight: 8.0
  PositionWeight: 1.0 (position 0)
  Contribution: 8.0 × 1 × 1.0 = 8.0

  ExactTokenMatch (skolgatan = skolgatan): +25.0

Token "31":
  Field: Address, Position: 1 (matches "31c" via prefix)
  FieldWeight: 8.0
  PositionWeight: 0.5 (position 1)
  Contribution: 8.0 × 1 × 0.5 = 4.0

  ExactTokenMatch: No (31 ≠ 31c)
  StartsWithBonus: No (numeric token)

BM25 Calculation:
  tfWeighted = 8.0 + 4.0 = 12.0
  IDF ≈ 3.5 (assuming moderate term frequency)
  BM25 ≈ 3.5 × (12.0 × 2.2) / (12.0 + 1.2 × 0.5) ≈ 7.3

Same-Field Multi-Token Bonus:
  Both tokens hit Address field: +40.0

Proximity Bonus:
  Tokens adjacent (span = 1): 1/(1+1) = 0.5

Term Match Bonus:
  2 tokens matched: 2 × 0.2 = 0.4

Final Score:
  0.0 + 7.3 + 25.0 + 40.0 + 0.5 + 0.4 = 73.2
```

---

## Design Decisions

### Why N-gram matches don't get Same-Field bonus

N-gram matches are inherently noisy (e.g., "hem" matching both "ålidhem" and "hemtjänst"). The same-field bonus rewards strong relevance signals, which n-gram matches are not. Excluding them prevents amplifying noise.

### Why numeric tokens don't get StartsWithBonus

When users search "31", they typically mean a street number, not a building name starting with "31". Excluding numeric tokens from StartsWithBonus prevents building "313-01" from ranking above an actual address match on "31".

### Why direct n-gram lookup instead of query expansion

Previously, the search generated n-grams FROM the query token ("stads" → "sta", "tad", "ads"...). This caused excessive noise as short n-grams matched many unrelated documents. Direct lookup ("stads" exists in index?) is more precise.

### Why IDF weighting for n-gram scores

Common Swedish substrings like "hem" (home) appear in many words. Without IDF weighting, they would contribute equal scores to rare substrings. IDF weighting reduces the impact of common n-grams.
