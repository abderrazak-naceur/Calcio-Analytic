# Mock File Provider

`MockFileProvider` (provider code **`mock`**) is a deterministic, offline provider
adapter that serves one complete, realistic vertical slice of sample data. It exists
so the whole pipeline — ingestion, odds-movement tracking, settlement and analysis —
can be exercised end-to-end without a real vendor.

It implements `IFootballProvider`, `IFixtureProvider`, `IOddsProvider` and
`IStatisticsProvider`.

## Where the data lives

Sample data is authored as JSON under `../../SampleData/` and embedded into the
assembly as `EmbeddedResource` (logical names `CalcioAnalytic.Ingestion.SampleData.*.json`).
`MockProviderData.Embedded` loads and caches it. You can also point at a folder of
the same JSON files with `MockProviderData.FromFolder(path)`.

## Registration

`AddMockProvider()` on `IServiceCollection` registers the adapter as an
`IProviderAdapter` singleton. It is additive and idempotent (uses `TryAddEnumerable`),
so it coexists with `AddIngestion()` and other providers. The existing `AddIngestion()`
method is unchanged.

```csharp
services.AddIngestion();
services.AddMockProvider(); // opt-in
```

## The sample data set

### Competition & season
- **Competition** `serie-a` — "Serie A" (Italy, tier 1)
- **Season** `serie-a-2024-2025` — "2024/2025" (2024-08-17 → 2025-05-25)

### Teams
- `inter` — FC Internazionale Milano ("Inter", IT)
- `juventus` — Juventus FC ("Juventus", IT)

### Match (deterministic result)
- **`match-001`** — Inter (home) vs Juventus (away)
- Competition `serie-a`, season `serie-a-2024-2025`
- Kickoff **2024-10-27T19:45:00Z**, venue Giuseppe Meazza, Matchday 9, referee Daniele Orsato
- Status **finished**, **final score 2–1** (half-time **1–0**)

### Events (goals)
- 23' `inter` (Lautaro Martinez)
- 58' `juventus` (Dusan Vlahovic)
- 79' `inter` (Marcus Thuram)

### Statistics (per team)
- possession: inter 54, juventus 46
- shots: inter 15, juventus 9
- shots_on_target: inter 6, juventus 3

### Bookmakers
- `bet365` — Bet365 (B365)
- `williamhill` — William Hill (WH)

### Markets
- `1x2` — Match Winner (Home / Draw / Away), period `FullTime`
- `ou25` — Over/Under 2.5 Goals, line 2.5, period `FullTime`

### Odds snapshots (price movement)
**6 pre-match snapshots** for `match-001` — **3 per bookmaker** — ordered opening →
mid → closing. The 1X2 home price shortens (Inter firms as favourite) while draw and
away drift, culminating in a closing snapshot just before kickoff:

| Bookmaker    | When (bookmaker ts) | Home | Draw | Away | O2.5 | U2.5 |
|--------------|---------------------|------|------|------|------|------|
| bet365       | 2024-10-25 19:45Z   | 2.10 | 3.40 | 3.60 | 1.95 | 1.90 |
| bet365       | 2024-10-27 17:45Z   | 1.95 | 3.50 | 3.90 | 1.90 | 1.95 |
| bet365       | 2024-10-27 19:40Z   | 1.85 | 3.60 | 4.20 | 1.88 | 1.98 |
| williamhill  | 2024-10-25 20:00Z   | 2.05 | 3.30 | 3.75 | 1.91 | 1.91 |
| williamhill  | 2024-10-27 17:30Z   | 1.90 | 3.45 | 4.00 | 1.87 | 1.96 |
| williamhill  | 2024-10-27 19:42Z   | 1.83 | 3.65 | 4.33 | 1.85 | 2.00 |

`GetOddsAsync` returns them ordered by bookmaker timestamp (then bookmaker id).

## RawPayloadHash / dedup

Every snapshot gets a stable `RawPayloadHash`: the lowercase SHA-256 of a canonical
string built from the bookmaker, match, live flag, minute, timestamps and every
market line/selection (ordered deterministically). The hash is independent of JSON
formatting, so identical odds always produce the same hash — downstream ingestion can
deduplicate reproducibly. See `MockProviderData.ComputeSnapshotHash`.
