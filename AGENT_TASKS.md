# Calcio-Analytic Agent Tasks

> Legenda: [x] fatto e committato su `main` · [~] parziale · [ ] da fare
> Stato aggiornato al commit `999a576` (Phase 7/8/9/13/14/15).

## P0 — Foundation and correctness

- [x] TASK-001 Create .NET 10 solution structure. (8 progetti + test, dependency rules pulite, build 0 warning)
- [x] TASK-002 Create React/Vite/Tailwind web application. (Tailwind v4, landing health, build ok)
- [~] TASK-003 Configure PostgreSQL, Redis and Docker Compose. (compose + CI presenti; non ancora avviato/verificato end-to-end con container)
- [x] TASK-004 Create base migrations and schemas. (InitialCreate + OddsStatsSettlementAnalytics; schemi catalog/matches/odds/statistics/analytics)
- [x] TASK-005 Implement provider abstraction. (IFootball/IFixture/IOdds/IStatistics/IStandings/IBookmaker + registry + options)
- [~] TASK-006 Implement football identity mappings. (entità ProviderEntityMap + repo + upsert idempotente; manca reconciliation/alias/dedup avanzati)
- [x] TASK-007 Implement match ingestion. (MatchIngestionService idempotente, test end-to-end mock: Inter 2-1 Juventus)
- [~] TASK-008 Implement bookmaker/market catalog. (CatalogIngestionService upserta bookmaker/market; manca sync schedulata/alias)
- [x] TASK-009 Implement immutable odds snapshots. (OddsIngestionService append-only + dedup PayloadHash + unique index)
- [ ] TASK-010 Implement result reconciliation. (cross-provider reconciliation non implementata)

## P0 — Match lifecycle

- [ ] TASK-011 Implement Scheduled -> PreMatch -> Live -> Finished lifecycle. (enum + mapping status presenti; transizioni/worker non implementati)
- [ ] TASK-012 Implement idempotent workers. (Worker ancora template; ingestion è idempotente ma non schedulata)
- [~] TASK-013 Implement post-match processing. (MatchAnalysisPersistenceService in corso; da completare e committare)
- [x] TASK-014 Implement settlement for supported markets. (SettlementEngine: 1X2 + Over/Under, Push, Unknown; servizio persistente in corso)
- [~] TASK-015 Implement analysis versioning. (MatchAnalysis versionato in dominio; servizio che incrementa la versione in corso)

## P1 — Analytics

- [x] TASK-016 Implement OddsAnalysisEngine. (OddsMath + OddsMovementAnalyzer)
- [x] TASK-017 Implement BookmakerAnalysisEngine. (BookmakerAnalyzer: best/worst/avg/dispersione)
- [x] TASK-018 Implement MarketAnalysisEngine. (MarketAnalyzer: overround + prob normalizzate)
- [x] TASK-019 Implement MatchAnalysisEngine. (report immutabile, methodology 1.0.0, JSON serializzato)
- [ ] TASK-020 Implement HistoricalPatternEngine.
- [ ] TASK-021 Implement SimilarMatchEngine.
- [ ] TASK-022 Implement read models.
- [~] TASK-023 Implement analytics APIs. (endpoint REST Phase 16 in corso: matches/odds/movement/bookmakers/analysis + ingestion POST)

## P1 — Web

- [~] TASK-024 Dashboard. (in corso: KPI + pannello ingest demo)
- [~] TASK-025 Match list. (in corso)
- [~] TASK-026 Match detail. (in corso: tab Overview/Odds/Movement/Bookmakers/Markets/Statistics)
- [ ] TASK-027 Odds movement screen. (dato esposto in Match Detail; schermo dedicato/explorer da fare)
- [ ] TASK-028 Bookmaker comparison. (dato esposto; schermo dedicato da fare)
- [ ] TASK-029 Historical pattern explorer.
- [ ] TASK-030 Similar matches.

## P2 — Models and scale

- [ ] TASK-031 Point-in-time feature store.
- [x] TASK-032 ELO baseline. (Python app/models/elo.py + endpoint + test)
- [x] TASK-033 Poisson baseline. (Python app/models/poisson.py + endpoint + test)
- [x] TASK-034 Dixon-Coles baseline. (Python app/models/dixon_coles.py + endpoint + test)
- [x] TASK-035 Backtesting engine. (Python app/backtesting: metriche + engine anti-leakage + endpoint)
- [ ] TASK-036 OpenTelemetry dashboards. (solo correlation-id lato API finora)
- [ ] TASK-037 Partition large tables.
- [ ] TASK-038 Customer API and API keys.
- [ ] TASK-039 AI-generated descriptive summaries.

## Riepilogo cosa manca (priorità)

1. Completare e committare: SettlementService + MatchAnalysisPersistenceService (chiude TASK-013/015).
2. Analytics REST API (TASK-023) e schermi web (TASK-024/025/026) — in corso.
3. Match lifecycle + worker schedulato idempotente (TASK-011/012).
4. Result reconciliation cross-provider (TASK-010).
5. HistoricalPatternEngine + SimilarMatchEngine + read models (TASK-020/021/022).
6. Explorer storici e comparazioni web (TASK-027/028/029/030).
7. Feature store point-in-time (TASK-031), integrazione modelli Python nel flusso .NET.
8. Osservabilità/OTel (TASK-036), partitioning (TASK-037), API clienti + API key (TASK-038), summaries AI (TASK-039).
9. Verifica Docker Compose end-to-end reale con Postgres/Redis (TASK-003).
10. Un provider reale (oltre al mock) quando disponibili credenziali.

## Rule

Never mark a task DONE without:
- implementation
- tests
- documentation
- verification against real or representative provider payloads
- preservation of historical correctness
