# Calcio-Analytic Agent Tasks

## Repository status — verified on main

- Main contains the merged Market Intelligence vertical slice.
- Latest CI verification is green across backend, frontend, Python analytics and Docker Compose validation.
- The former `feature/platform-completion-phase-1` branch no longer contains commits ahead of `main`; its work is integrated in main.
- The former `feature/high-odds-intelligence` branch also has no unique commits ahead of main; its work is already integrated.
- SportMonks remains the primary provider, but DATA-001 stays partial until a real credential/subscription is validated against representative odds coverage and historical depth.


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
- [~] MARKET-004 Favorite-failure analysis: API/dashboard implemented with favorite-odds ranges and winner-odds thresholds; automated CI is green, while real-provider validation remains pending.
- [~] MARKET-005 1X2 historical market analysis. First historical read endpoint implemented; broader backfill/robustness remains.
- [~] MARKET-006 Over/Under 1.5 / 2.5 / 3.5 analysis. Settlement/API path is generic; representative 2.5 fixture exists.
- [~] MARKET-007 BTTS YES/NO analysis. Settlement and representative fixture added; historical aggregation remains.
- [x] MARKET-008 Historical odds-range statistics: sample, wins, losses, hit/miss rate, upset rate, implied vs actual probability, flat-stake P/L and ROI. Implemented for Market vs Reality with deterministic statistics tests and dashboard presentation.
- [x] MARKET-009 Upcoming analysis: today, tomorrow, 2/7/14/30 days using comparable historical situations. Added 1X2 upcoming endpoint, historical comparable statistics and dashboard view with minimum-sample filtering.
- [x] MARKET-010 Dedicated Market Failures and Upsets views. Market Failures and dedicated Upsets routes are live and use the market-outcome read API.
- [x] MARKET-011 Main Analysis Dashboard connected to market-outcome read API; the dashboard now surfaces live 1X2 Market vs Reality KPIs with period and upset-threshold controls.
- [x] MARKET-012 Match Detail market-outcome and odds-history presentation. Market Reality is live in Match Detail; deeper odds-history presentation remains a separate data-depth enhancement.
- [x] MARKET-013 Daily Market Report. Added daily 1X2 aggregation API, upcoming count and top upset rows, with full-slice automated coverage and dashboard route.
- [x] MARKET-014 Data-quality visibility for missing/incomplete odds/results. Added global quality summary API and Data Quality dashboard for odds, results, settlements, duplicate snapshots and stale scheduled matches.

### P2 — SportMonks depth and historical coverage

- [~] DATA-001 Validate the real SportMonks integration and supported odds/market endpoints. Adapter/controller/tests are present; real token, subscription/market coverage and historical depth still need verification.
- [ ] DATA-002 Historical backfill for configurable periods.
- [ ] DATA-003 Persist opening/current/pre-kickoff/closing/min/max odds without overwriting snapshots.
- [x] DATA-004 Provider/bookmaker/market provenance and completeness checks. Odds snapshots now persist the supplying provider directly, with FK/index integrity, provenance coverage metrics in Data Quality, and end-to-end verification.
- [x] DATA-005 Representative SportMonks payload integration tests. Existing provider tests cover leagues, pagination, fixtures/results/teams, 1X2 odds mapping and missing-token errors with representative JSON payloads.
- [x] DATA-006 Scheduled odds/result synchronization respecting provider limits. Added opt-in SportMonks worker with bounded batches, configurable lookahead/recent windows, minimum request delay and safe disabled-by-default behavior when no token is configured.

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

## Current next execution order

1. Validate the real SportMonks connection, subscribed markets and historical odds depth.
2. Harden historical Market vs Reality aggregation for 1X2, Over/Under and BTTS.
3. Implement historical odds-range statistics including P/L and ROI.
4. ~~Implement Upcoming Analysis for today/2/7/14/30-day windows.~~ Done for the current 1X2 market slice.
5. Complete dedicated Upsets view and richer dashboard/read models.
6. Add Daily Market Report and scheduled jobs.
7. Add historical backfill and provenance hardening; data-quality visibility is now available.
8. Only then expand advanced model comparison and SaaS capabilities.

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
