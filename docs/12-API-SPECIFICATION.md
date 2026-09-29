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
GET data-quality
GET ingestion/runs

## Standards
Pagination, filtering, sorting, date ranges, consistent errors, correlation IDs e rate limits.

## Versioning
Usare /api/v1 prima del lancio della public API.

## Authentication
User session per web; API keys/OAuth per clienti esterni.
