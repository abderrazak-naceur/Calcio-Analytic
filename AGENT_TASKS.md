# Calcio-Analytic Agent Tasks

> Legenda: [x] fatto e committato su `main` · [~] parziale · [ ] da fare
> Stato aggiornato al commit `9cbd240` (Phase 11/12/16/18/20/24 + worker lifecycle).

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

- [~] TASK-011 Implement Scheduled -> PreMatch -> Live -> Finished lifecycle. (worker transiziona Finished->Analyzed; transizioni pre-match/live legate a ingestione live ancora da fare)
- [x] TASK-012 Implement idempotent workers. (PostMatchProcessingWorker: scoped, resiliente, idempotente)
- [x] TASK-013 Implement post-match processing. (SettlementService + MatchAnalysisPersistenceService, guidati dal worker; test verdi)
- [x] TASK-014 Implement settlement for supported markets. (SettlementEngine + SettlementService persistente idempotente)
- [x] TASK-015 Implement analysis versioning. (MatchAnalysisPersistenceService: versione = max+1, righe immutabili; test)

## P1 — Analytics

- [x] TASK-016 Implement OddsAnalysisEngine. (OddsMath + OddsMovementAnalyzer)
- [x] TASK-017 Implement BookmakerAnalysisEngine. (BookmakerAnalyzer: best/worst/avg/dispersione)
- [x] TASK-018 Implement MarketAnalysisEngine. (MarketAnalyzer: overround + prob normalizzate)
- [x] TASK-019 Implement MatchAnalysisEngine. (report immutabile, methodology 1.0.0, JSON serializzato)
- [x] TASK-020 Implement HistoricalPatternEngine. (query riproducibile con hash, distribuzioni, Wilson CI)
- [x] TASK-021 Implement SimilarMatchEngine. (kNN pesato su feature normalizzate, spiegazioni, anti-leakage documentato)
- [ ] TASK-022 Implement read models. (proiezioni ottimizzate per dashboard non ancora create)
- [~] TASK-023 Implement analytics APIs. (matches/odds/movement/bookmakers/analysis + ingestion POST fatti; mancano endpoint patterns/similar/backtests)

## P1 — Web

- [x] TASK-024 Dashboard. (KPI + pannello ingest demo, health chip)
- [x] TASK-025 Match list. (tabella + filtro stato + navigazione)
- [x] TASK-026 Match detail. (tab Overview/Odds/Movement/Bookmakers/Markets/Statistics)
- [ ] TASK-027 Odds movement screen. (dato esposto in Match Detail; schermo/explorer dedicato da fare)
- [ ] TASK-028 Bookmaker comparison. (dato esposto; schermo dedicato da fare)
- [ ] TASK-029 Historical pattern explorer.
- [ ] TASK-030 Similar matches.

## P2 — Models and scale

- [ ] TASK-031 Point-in-time feature store.
- [x] TASK-032 ELO baseline. (Python app/models/elo.py + endpoint + test)
- [x] TASK-033 Poisson baseline. (Python app/models/poisson.py + endpoint + test)
- [x] TASK-034 Dixon-Coles baseline. (Python app/models/dixon_coles.py + endpoint + test)
- [x] TASK-035 Backtesting engine. (Python app/backtesting: metriche + engine anti-leakage + endpoint)
- [ ] TASK-036 OpenTelemetry dashboards. (correlation-id + security headers lato API; OTel tracing/metrics da fare)
- [ ] TASK-037 Partition large tables.
- [~] TASK-038 Customer API and API keys. (API key filter + rate limiting lato API; gestione chiavi/quote per cliente da fare)
- [ ] TASK-039 AI-generated descriptive summaries.

## Riepilogo cosa manca (priorità)

1. Endpoint analytics per patterns/similar/backtests (completa TASK-023).
2. Read models/proiezioni per dashboard (TASK-022).
3. Explorer storici e comparazioni web (TASK-027/028/029/030).
4. Result reconciliation cross-provider (TASK-010).
5. Transizioni lifecycle pre-match/live legate a ingestione live (completa TASK-011).
6. Feature store point-in-time (TASK-031) + integrazione modelli Python nel flusso .NET.
7. Osservabilità/OTel (TASK-036), partitioning (TASK-037), API clienti + quote (TASK-038), summaries AI (TASK-039).
8. SaaS multi-utente (org/ruoli/quote).
9. Verifica Docker Compose end-to-end reale con Postgres/Redis (TASK-003).
10. Un provider reale (oltre al mock) quando disponibili credenziali.

## Rule

Never mark a task DONE without:
- implementation
- tests
- documentation
- verification against real or representative provider payloads
- preservation of historical correctness
