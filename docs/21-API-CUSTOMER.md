# 21 — Customer API Specification

## Purpose
The customer API exposes normalized football, match, odds, historical and analytics data without leaking provider-specific schemas.

## Principles
- Provider-neutral contracts.
- Stable IDs and pagination.
- Point-in-time correctness.
- Explicit timestamps and data freshness.
- Read APIs must never mutate analytical history.
- Raw provider payloads are internal unless licensing permits exposure.

## Core resources
- competitions
- seasons
- teams
- players
- matches
- bookmakers
- markets
- odds
- odds-history
- statistics
- standings
- team-form
- h2h
- analyses
- similar-matches
- historical-patterns
- backtests

## Match endpoints
GET /api/v1/matches
GET /api/v1/matches/{matchId}
GET /api/v1/matches/{matchId}/events
GET /api/v1/matches/{matchId}/statistics
GET /api/v1/matches/{matchId}/standings-context
GET /api/v1/matches/{matchId}/form
GET /api/v1/matches/{matchId}/h2h
GET /api/v1/matches/{matchId}/analysis

## Odds endpoints
GET /api/v1/matches/{matchId}/odds
GET /api/v1/matches/{matchId}/odds/history
GET /api/v1/matches/{matchId}/odds/opening
GET /api/v1/matches/{matchId}/odds/closing
GET /api/v1/matches/{matchId}/odds/movement
GET /api/v1/matches/{matchId}/bookmakers
GET /api/v1/markets/{marketId}/history

## Analytics endpoints
GET /api/v1/analytics/matches/{matchId}
GET /api/v1/analytics/matches/{matchId}/similar
POST /api/v1/analytics/patterns/query
POST /api/v1/analytics/backtests
GET /api/v1/analytics/backtests/{runId}
GET /api/v1/analytics/dashboard

## Response requirements
Every time-dependent response should expose:
- capturedAt
- providerUpdatedAt when available
- source/provider
- dataQuality
- completeness
- isLive where relevant

## Pattern query
A pattern query may filter by:
- competition/season
- home/away
- market
- opening odds range
- closing odds range
- movement percentage
- bookmaker
- line
- team strength/rating bucket
- form bucket
- historical date range

The result must contain sample size, filters, outcome distribution, confidence intervals where applicable, and methodology metadata.

## Backtesting
Backtesting must use only information available at the simulated decision timestamp. Future results and future odds must never leak into features.

## Errors
Use stable error codes:
- INVALID_REQUEST
- NOT_FOUND
- PROVIDER_DATA_UNAVAILABLE
- DATA_INCOMPLETE
- ANALYSIS_NOT_READY
- RATE_LIMITED
- INTERNAL_ERROR

## API key security

Write endpoints may require an API key credential. Customer keys must never
be persisted as plaintext: generation returns the secret once, while the
persisted representation contains a SHA-256 hash, a short key prefix, scopes
and revocation/audit timestamps. Validation uses fixed-time comparison and
rejects revoked keys. The current implementation retains the legacy
configuration-key path for existing local deployments; PostgreSQL-backed
customer key CRUD, tenant association, quotas and full audit history remain
an explicit follow-up.
