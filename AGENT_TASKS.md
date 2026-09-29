# Calcio-Analytic Agent Tasks

> Legenda: [x] fatto e committato su `main` · [~] parziale · [ ] da fare
> Stato aggiornato al commit `f7621da` (Phase 9/11/12/16/19/20/21/22/23/24/28 + worker + OTel + reconciliation).

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
- [x] TASK-010 Implement result reconciliation. (ResultReconciliationService: Reconciled/Unconfirmed/Conflict; non sovrascrive mai, idempotente)

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
- [~] TASK-022 Implement read models. (dashboard summary/recent via query aggregate; materialized view a scala da fare)
- [x] TASK-023 Implement analytics APIs. (matches/odds/movement/bookmakers/analysis + patterns/query + similar + backtests proxy + ingestion POST)

## P1 — Web

- [x] TASK-024 Dashboard. (KPI + pannello ingest demo, health chip)
- [x] TASK-025 Match list. (tabella + filtro stato + navigazione)
- [x] TASK-026 Match detail. (tab Overview/Odds/Movement/Bookmakers/Markets/Statistics)
- [~] TASK-027 Odds movement screen. (tab Movement in Match Detail; explorer storico dedicato ancora da fare)
- [~] TASK-028 Bookmaker comparison. (tab Bookmakers in Match Detail; schermo comparativo dedicato da fare)
- [x] TASK-029 Historical pattern explorer. (PatternExplorer con filtri URL-shareable)
- [x] TASK-030 Similar matches. (pagina SimilarMatches + link da Match Detail)

## P2 — Models and scale

- [ ] TASK-031 Point-in-time feature store.
- [x] TASK-032 ELO baseline. (Python app/models/elo.py + endpoint + test)
- [x] TASK-033 Poisson baseline. (Python app/models/poisson.py + endpoint + test)
- [x] TASK-034 Dixon-Coles baseline. (Python app/models/dixon_coles.py + endpoint + test)
- [x] TASK-035 Backtesting engine. (Python app/backtesting: metriche + engine anti-leakage + endpoint)
- [~] TASK-036 OpenTelemetry dashboards. (OTel tracing+metrics con export OTLP opzionale; dashboards/collector di produzione da configurare)
- [ ] TASK-037 Partition large tables.
- [~] TASK-038 Customer API and API keys. (API key filter + rate limiting lato API; gestione chiavi/quote per cliente da fare)
- [~] TASK-039 AI-generated descriptive summaries. (endpoint Python /api/v1/summaries/match: deterministico, tracciabile, non predittivo; proxy .NET + UI da fare)

## Nota: Data quality (Phase 21)
DataQualityEngine (8 check + score dq-1.0.0) + GET api/v1/dataquality/matches/{id} fatti.

## Riepilogo cosa manca (priorità)

1. Feature store point-in-time (TASK-031) + integrazione modelli Python nel flusso .NET (rating/form reali per similarity).
2. Dashboard UI collegata al read-model (summary/recent) + pannello Data Quality; explorer dedicati movement/bookmaker (TASK-027/028).
3. Proxy .NET + UI per AI summaries (completa TASK-039).
4. Transizioni lifecycle pre-match/live legate a ingestione live (completa TASK-011).
5. SaaS multi-utente (org/ruoli/quote) — Phase 27; API clienti + quote (TASK-038).
6. Partitioning tabelle grandi (TASK-037); materialized read model a scala (completa TASK-022).
7. Dashboards OTel di produzione/collector (completa TASK-036).
8. Data quality aggregata per provider/competizione/giorno (estende Phase 21).
9. Verifica Docker Compose end-to-end reale con Postgres/Redis (TASK-003).
10. Un provider reale (oltre al mock) quando disponibili credenziali.

## Rule

Never mark a task DONE without:
- implementation
- tests
- documentation
- verification against real or representative provider payloads
- preservation of historical correctness
