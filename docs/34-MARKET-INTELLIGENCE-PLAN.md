# 34 — Market Intelligence Product Plan

## Product objective

Calcio-Analytic is a **Football Market Intelligence / Analysis Dashboard**.

The core question is:

> What did the market/bookmaker expect, and what actually happened?

The system is not primarily a prediction engine. It first measures the historical relationship between market prices and real outcomes.

## Core flow

```
SportMonks
  ↓
Matches / Results
  ↓
Odds snapshots
  ↓
Market normalization
  ↓
Market outcome
  ↓
HIT / MISS / UPSET
  ↓
Historical statistics
  ↓
Upcoming comparable situations
  ↓
Analysis Dashboard
```

## Core example — 1X2

If:

- Home = 1.60
- Draw = 4.20
- Away = 6.50

and Away wins:

- favorite = Home @ 1.60
- favorite result = LOST
- market = MISS
- winner = Away @ 6.50
- classification = UPSET

The platform must aggregate thousands of such cases.

### Favorite failure analysis

For a favorite range such as 1.50–1.70, calculate:

- total matches;
- favorite wins/losses;
- failure rate;
- winner-odds distribution when the favorite loses;
- frequency of winners at 3+, 5+, 6+, 7+, 8+, 10+;
- sample size and statistical confidence;
- historical P/L and ROI where requested.

## Other markets

The same Market vs Reality engine must support:

### Over/Under

- Over 1.5 / Under 1.5
- Over 2.5 / Under 2.5
- Over 3.5 / Under 3.5

Example: Over 2.5 @ 1.50 loses and Under @ 2.60 wins → Over = MISS, Under = HIT.

### BTTS / Goal

- BTTS YES
- BTTS NO

The design must remain generic so future markets such as corners, cards and handicaps can be added without duplicating settlement logic.

## Odds history

Every available odds snapshot is immutable.

Store:

- match;
- bookmaker;
- market;
- selection;
- line;
- odd;
- provider timestamp;
- ingestion timestamp;
- source/provenance.

Derive, without overwriting history:

- opening;
- current;
- latest pre-kickoff;
- closing;
- minimum;
- maximum;
- movement percentage.

Pre-match analysis must enforce:

`CapturedAtUtc < KickoffUtc`

Post-kickoff prices must never be used as pre-match evidence.

## Historical analysis

Historical market outcome statistics now expose sample size, wins/losses, favorite hit rate,
average implied probability, actual hit rate, one-unit flat-stake profit/loss and ROI for
favorite-odds ranges. The calculation excludes UNKNOWN outcomes and invalid odds and uses only
pre-kickoff settled observations from a single bookmaker/market line.

Users must be able to filter by:

- date range;
- competition/season;
- bookmaker;
- market;
- selection;
- favorite odds range;
- winner odds threshold;
- opening/closing odds;
- movement.

Return:

- sample size;
- wins/losses;
- HIT/MISS rate;
- UPSET count/rate;
- implied probability;
- actual probability;
- difference;
- average/min/max odds;
- flat-stake P/L and ROI;
- confidence/sample-quality indicator.

## Upcoming analysis

The first production slice is available at `/api/v1/analytics/upcoming` and in the
Upcoming Analysis UI. It supports Today, Tomorrow, 2, 7, 14 and 30-day windows for
1X2. Each upcoming bookmaker/market line is matched to historical 1X2 situations in
the same favorite-odds bucket and reports comparable sample size, hit/failure rate,
upsets, implied versus actual probability and flat-stake ROI. Results below the
minimum comparable sample threshold are excluded from the UI. The endpoint only uses
pre-kickoff snapshots for the upcoming match and settled historical observations.

Analyze:

- today;
- tomorrow;
- next 2 days;
- next 7 days;
- next 14 days;
- next 30 days.

For each upcoming market, show current odds and comparable historical statistics.

The system must say:

> In N comparable historical situations, the favorite won X% and lost Y%.

It must **not** claim that the future result is guaranteed.

## Dashboard

The main Analysis Dashboard should expose:

- matches analyzed;
- odds snapshots;
- market hits;
- market misses;
- upsets;
- high-odds wins;
- favorite failure rate;
- today's finished results;
- current/upcoming odds movements;
- biggest market failures;
- biggest upsets.

Dedicated views:

- Market Failures;
- Upsets;
- Historical Analysis;
- Upcoming Analysis;
- Odds Movement;
- Match Detail;
- Daily Reports.

The UI should feel like a professional market-intelligence terminal, not a simple results table.

## SportMonks

SportMonks is the primary provider.

Before implementing provider-specific behavior:

1. verify actual available endpoints;
2. verify historical odds depth;
3. verify supported markets;
4. verify bookmaker coverage;
5. verify timestamps;
6. document limitations and provenance.

Never invent API endpoints or fabricate missing data.

## Advanced models

ELO, Poisson and Dixon-Coles remain valuable, but are **Phase 2 / Advanced Analytics**.

They should eventually answer:

`model probability vs market implied probability vs actual result`

They must not block the core Market Intelligence pipeline.

## Production automation

Required jobs:

- sync upcoming matches;
- sync odds snapshots;
- sync results;
- reconcile market outcomes;
- calculate historical aggregates;
- generate daily reports;
- perform historical backfill.

Respect SportMonks rate limits and preserve idempotency.

## Quality rules

Never:

- overwrite historical odds;
- use post-kickoff odds for pre-match analysis;
- fabricate statistics;
- hide missing data;
- treat tiny samples as reliable;
- present historical statistics as guaranteed predictions.

Every displayed statistic must be traceable to stored matches, odds and results.

## Acceptance milestone

The first important end-to-end milestone is:

**One real SportMonks match → complete odds history → final result → automatic HIT/MISS/UPSET → historical aggregation → API → dashboard.**

Only after this vertical slice is reliable should the project expand into advanced model comparison and SaaS features.


## Current implementation status

The first Market vs Reality vertical slice is now merged into `main`:

- pure Favorite / HIT / MISS / UPSET classification;
- configurable upset threshold;
- BTTS settlement support;
- historical market-outcome API for 1X2, OU and BTTS;
- strict pre-kickoff evidence rule: `ProviderTimestampUtc < KickoffUtc`;
- bookmaker-specific grouping without mixing prices between bookmakers;
- standard winner-odds thresholds 5+, 6+, 7+, 8+, 10+;
- favorite-odds failure ranges;
- Market vs Reality dashboard card and dedicated Market Failures page;
- Match Detail Market Reality presentation;
- automated CI verification across backend, frontend, Python analytics and Docker Compose.

The API currently uses the latest available pre-kickoff snapshot for each
bookmaker/market/selection. Closing/opening derivation and deeper historical
backfill remain separate data-depth tasks.

## Verified repository state

The implementation described above is currently integrated in `main`. The remaining gaps are primarily real SportMonks validation, historical data depth/backfill, richer read models, Upcoming Analysis, Daily Reports and data-quality hardening. These are intentionally kept separate from the already working Market vs Reality vertical slice.
