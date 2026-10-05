# Calcio-Analytic Agent Tasks

> Legenda: [x] fatto e verificato · [~] parziale · [ ] da fare · [!] bloccato
> Piano riallineato al prodotto **Football Market Intelligence / Analysis Dashboard**.
> La priorità è Market vs Reality: quote SportMonks → storico quote → risultato → HIT/MISS/UPSET → analisi storica → analisi future → dashboard.

## Product priority

Il core del prodotto è:

```
SportMonks
  → Matches / Results
  → Odds Snapshots
  → Market Normalization
  → Market Outcome
  → HIT / MISS / UPSET
  → Historical Market Statistics
  → Upcoming Analysis
  → Analysis Dashboard
```

ELO / Poisson / Dixon-Coles restano **Advanced Analytics / Phase 2** e non devono bloccare il core.

## Current verified backlog

### P0 — Foundation and data correctness

- [x] TASK-001 Create .NET 10 solution structure.
- [x] TASK-002 Create React/Vite/Tailwind web application.
- [~] TASK-003 Configure PostgreSQL, Redis and Docker Compose.
- [x] TASK-004 Create base migrations and schemas.
- [x] TASK-005 Implement provider abstraction.
- [~] TASK-005A Implement compliant web-data acquisition layer. Keep secondary; SportMonks is the primary source.
- [~] TASK-006 Implement football identity mappings.
- [x] TASK-007 Implement match ingestion.
- [~] TASK-008 Implement bookmaker/market catalog.
- [x] TASK-009 Implement immutable odds snapshots.
- [x] TASK-010 Implement result reconciliation.

### P0 — Match lifecycle and settlement

- [~] TASK-011 Implement Scheduled → PreMatch → Live → Finished lifecycle.
- [x] TASK-012 Implement idempotent workers.
- [x] TASK-013 Implement post-match processing.
- [x] TASK-014 Implement settlement for supported markets.
- [x] TASK-015 Implement analysis versioning.

### P1 — Core market intelligence

- [x] TASK-016 Implement OddsAnalysisEngine.
- [x] TASK-017 Implement BookmakerAnalysisEngine.
- [x] TASK-018 Implement MarketAnalysisEngine.
- [x] TASK-019 Implement MatchAnalysisEngine.
- [x] TASK-020 Implement HistoricalPatternEngine.
- [x] TASK-021 Implement SimilarMatchEngine.
- [~] TASK-022 Implement optimized read models.
- [x] TASK-023 Implement analytics APIs.
- [~] TASK-027 Odds movement explorer.
- [~] TASK-028 Bookmaker comparison explorer.
- [x] TASK-029 Historical pattern explorer.
- [x] TASK-030 Similar matches.

### P1 — Market outcome intelligence — next priority

These capabilities must be treated as first-class product requirements, using existing implementations where possible and adding missing tests/contracts/UI rather than duplicating engines.

- [x] MARKET-001 Favorite detection for supported markets. Pure engine + deterministic tests.
- [x] MARKET-002 Generic HIT / MISS classification. Pure engine + deterministic tests.
- [x] MARKET-003 UPSET classification with configurable thresholds (default 5+, 6+, 7+, 8+, 10+). Engine supports configurable threshold; API exposes the standard 5/6/7/8/10 breakdown.
- [~] MARKET-004 Favorite-failure analysis: API/dashboard implemented with favorite-odds ranges and winner-odds thresholds; CI/integration verification pending.
- [~] MARKET-005 1X2 historical market analysis. First historical read endpoint implemented; broader backfill/robustness remains.
- [~] MARKET-006 Over/Under 1.5 / 2.5 / 3.5 analysis. Settlement/API path is generic; representative 2.5 fixture exists.
- [~] MARKET-007 BTTS YES/NO analysis. Settlement and representative fixture added; historical aggregation remains.
- [ ] MARKET-008 Historical odds-range statistics: sample, wins, losses, hit/miss rate, upset rate, implied vs actual probability, flat-stake P/L and ROI.
- [ ] MARKET-009 Upcoming analysis: today, tomorrow, 2/7/14/30 days using comparable historical situations.
- [~] MARKET-010 Dedicated Market Failures and Upsets views. Market Failures route is live; dedicated Upsets view remains.
- [~] MARKET-011 Main Analysis Dashboard connected to market-outcome read API; richer production read models remain.
- [ ] MARKET-012 Match Detail market-outcome and odds-history presentation.
- [ ] MARKET-013 Daily Market Report.
- [ ] MARKET-014 Data-quality visibility for missing/incomplete odds/results.

### P2 — SportMonks depth and historical coverage

- [~] DATA-001 Validate the real SportMonks integration and supported odds/market endpoints. Adapter/controller/tests are present; real token, subscription/market coverage and historical depth still need verification.
- [ ] DATA-002 Historical backfill for configurable periods.
- [ ] DATA-003 Persist opening/current/pre-kickoff/closing/min/max odds without overwriting snapshots.
- [ ] DATA-004 Provider/bookmaker/market provenance and completeness checks.
- [ ] DATA-005 Representative SportMonks payload integration tests.
- [ ] DATA-006 Scheduled odds/result synchronization respecting provider limits.

### P2 — Advanced analytics

- [~] TASK-031 Point-in-time feature store. Foundation exists; population/integration remains.
- [x] TASK-032 ELO baseline.
- [x] TASK-033 Poisson baseline.
- [x] TASK-034 Dixon-Coles baseline.
- [x] TASK-035 Backtesting engine.
- [~] TASK-036 OpenTelemetry dashboards.
- [ ] TASK-037 Partition large tables.
- [~] TASK-038 Customer API and API keys.
- [~] TASK-039 AI-generated descriptive summaries.
- [x] TASK-040 High Odds Intelligence.

Advanced models must be used to compare model probability vs market probability only after the core market-intelligence pipeline is stable. They must not replace the historical Market vs Reality analysis.

### P2 — Production / SaaS

- Complete Docker/CI/E2E verification with real PostgreSQL + Redis.
- Complete customer/API-key management, roles, quotas and audit logging.
- Add partitioning/materialized read models when justified by measured scale.
- Complete production OTel dashboards/collector.
- Complete AI descriptive-summary proxy/UI.
- Production deployment, backups/restore tests, alerts, rate limits and security review.

## Correct execution order

1. SportMonks real-data validation.
2. Match + result + odds snapshot vertical slice.
3. Market normalization and settlement.
4. HIT / MISS / UPSET engine.
5. Favorite-failure + high-odds analysis.
6. 1X2 + Over/Under + BTTS.
7. Historical statistics and odds ranges.
8. Upcoming analysis.
9. Analysis Dashboard + Market Failures + Upsets + Match Detail.
10. Daily reports and scheduled automation.
11. Historical backfill and data-quality hardening.
12. Only then expand advanced ELO/Poisson/Dixon-Coles usage.

## Definition of DONE

A task is DONE only when:
- implementation exists;
- automated tests exist and pass;
- documentation is updated;
- CI passes;
- real or representative provider payloads are verified;
- historical/pre-kickoff correctness is preserved;
- no critical data-quality issue remains.

Do not mark a task DONE merely because a class, migration, endpoint or UI screen exists.
