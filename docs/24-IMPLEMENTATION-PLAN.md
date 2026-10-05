# 24 — Implementation Plan

## Phase 0 — Foundation
- repository conventions
- AGENTS.md
- AGENT_TASKS.md
- Docker Compose
- .NET 10 solution
- React/Vite/Tailwind application
- PostgreSQL
- Redis
- migrations
- health checks
- OpenTelemetry

## Phase 1 — Catalog
Implement:
- countries
- competitions
- seasons
- teams
- players
- bookmakers
- markets
- provider mappings

## Phase 2 — Match ingestion
Implement:
- fixtures
- team relationships
- match status
- events
- statistics
- standings snapshots
- form snapshots
- H2H snapshots

## Phase 3 — Odds ingestion
Implement:
- provider adapters
- bookmaker mappings
- market mappings
- lines
- selections
- opening odds
- every odds snapshot
- closing odds
- movement events

## Phase 4 — Match lifecycle
States:
Scheduled -> PreMatch -> Live -> Finished -> Analyzed -> Reconciled.

Every transition is idempotent and auditable.

## Phase 5 — Analytics Engine
Implement:
- MatchAnalysisEngine
- OddsAnalysisEngine
- BookmakerAnalysisEngine
- MarketAnalysisEngine
- TeamAnalysisEngine
- HistoricalPatternEngine
- SimilarMatchEngine
- ModelEvaluationEngine

## Phase 6 — Post-match automation
When a match becomes Finished:
1. verify final result
2. collect final statistics
3. finalize closing odds
4. settle supported markets
5. calculate movement metrics
6. generate analysis
7. update aggregates
8. index for similar-match search
9. mark analysis complete

## Phase 7 — Web application
Screens:
- Dashboard
- Competitions
- Matches
- Match detail
- Odds explorer
- Bookmaker explorer
- Market explorer
- Historical patterns
- Similar matches
- Backtesting
- Data quality
- Admin

## Phase 8 — Backtesting
Implement point-in-time feature store and reproducible runs.

## Phase 9 — Scale
- partition large tables
- archive raw payloads
- optimize indexes
- queue heavy analytics
- introduce read models/materialized aggregates

## Current Market Intelligence priority

The repository has moved beyond the original generic phase sequence. The active implementation priority is:

1. Real SportMonks validation and representative odds coverage.
2. Historical Market vs Reality aggregation for 1X2, Over/Under and BTTS.
3. Odds-range statistics, P/L and ROI.
4. Upcoming comparable-situation analysis.
5. Market Failures, Upsets and Match Detail hardening.
6. Daily reports, data-quality visibility and scheduled automation.
7. Historical backfill and optimized read models.
8. Advanced ELO/Poisson/Dixon-Coles comparison.

The original phases remain useful as architectural guidance, but this Market Intelligence sequence is the current execution order.

## Priority
P0: data correctness, timestamps, identity mapping, odds snapshots, result lifecycle.
P1: Market vs Reality analysis, historical explorer, upcoming analysis, match detail.
P2: advanced models, AI summaries, customer API, billing.

## Definition of done
No analytical feature is accepted if its output cannot be reproduced from stored historical inputs.
