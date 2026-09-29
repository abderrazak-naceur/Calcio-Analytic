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

## Priority
P0: data correctness, timestamps, identity mapping, odds snapshots, result lifecycle.
P1: analysis engine, historical explorer, match detail.
P2: advanced models, AI summaries, customer API, billing.

## Definition of done
No analytical feature is accepted if its output cannot be reproduced from stored historical inputs.
