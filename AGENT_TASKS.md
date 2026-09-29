# Calcio-Analytic Agent Tasks

## P0 — Foundation and correctness

- [ ] TASK-001 Create .NET 10 solution structure.
- [ ] TASK-002 Create React/Vite/Tailwind web application.
- [ ] TASK-003 Configure PostgreSQL, Redis and Docker Compose.
- [ ] TASK-004 Create base migrations and schemas.
- [ ] TASK-005 Implement provider abstraction.
- [ ] TASK-006 Implement football identity mappings.
- [ ] TASK-007 Implement match ingestion.
- [ ] TASK-008 Implement bookmaker/market catalog.
- [ ] TASK-009 Implement immutable odds snapshots.
- [ ] TASK-010 Implement result reconciliation.

## P0 — Match lifecycle

- [ ] TASK-011 Implement Scheduled -> PreMatch -> Live -> Finished lifecycle.
- [ ] TASK-012 Implement idempotent workers.
- [ ] TASK-013 Implement post-match processing.
- [ ] TASK-014 Implement settlement for supported markets.
- [ ] TASK-015 Implement analysis versioning.

## P1 — Analytics

- [ ] TASK-016 Implement OddsAnalysisEngine.
- [ ] TASK-017 Implement BookmakerAnalysisEngine.
- [ ] TASK-018 Implement MarketAnalysisEngine.
- [ ] TASK-019 Implement MatchAnalysisEngine.
- [ ] TASK-020 Implement HistoricalPatternEngine.
- [ ] TASK-021 Implement SimilarMatchEngine.
- [ ] TASK-022 Implement read models.
- [ ] TASK-023 Implement analytics APIs.

## P1 — Web

- [ ] TASK-024 Dashboard.
- [ ] TASK-025 Match list.
- [ ] TASK-026 Match detail.
- [ ] TASK-027 Odds movement screen.
- [ ] TASK-028 Bookmaker comparison.
- [ ] TASK-029 Historical pattern explorer.
- [ ] TASK-030 Similar matches.

## P2 — Models and scale

- [ ] TASK-031 Point-in-time feature store.
- [ ] TASK-032 ELO baseline.
- [ ] TASK-033 Poisson baseline.
- [ ] TASK-034 Dixon-Coles baseline.
- [ ] TASK-035 Backtesting engine.
- [ ] TASK-036 OpenTelemetry dashboards.
- [ ] TASK-037 Partition large tables.
- [ ] TASK-038 Customer API and API keys.
- [ ] TASK-039 AI-generated descriptive summaries.

## Rule

Never mark a task DONE without:
- implementation
- tests
- documentation
- verification against real or representative provider payloads
- preservation of historical correctness
