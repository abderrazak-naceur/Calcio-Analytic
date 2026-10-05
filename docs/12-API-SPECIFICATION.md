# 12 — API Specification

## Resources
GET competitions
GET seasons
GET teams
GET players
GET matches
GET matches/{id}
GET matches/{id}/odds
GET matches/{id}/statistics
GET odds
GET odds/movements
GET standings
GET analytics/teams
GET analytics/markets
GET analytics/backtests
GET analytics/market-outcomes
GET data-quality
GET ingestion/runs

## Standards
Pagination, filtering, sorting, date ranges, consistent errors, correlation IDs e rate limits.

## Versioning
Usare /api/v1 prima del lancio della public API.

## Authentication
User session per web; API keys/OAuth per clienti esterni.

## Market Intelligence endpoints

### GET /api/v1/analytics/market-outcomes

Historical Market vs Reality read API supporting:

- `matchId`, `fromUtc`, `toUtc`;
- market code: `1X2`, `OU`, `BTTS`;
- bookmaker filtering;
- favorite-odds and winner-odds thresholds;
- HIT / MISS / UPSET classification;
- pagination;
- pre-kickoff evidence only (`ProviderTimestampUtc < KickoffUtc`).

The response exposes aggregate market outcomes, favorite failure rate, winner-odds threshold breakdowns and favorite-odds ranges.
