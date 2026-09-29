# Calcio-Analytic — Product, Functional & Business Plan

## Executive Summary

Calcio-Analytic è una piattaforma di football data intelligence che raccoglie, normalizza, storicizza e analizza dati calcistici provenienti da fonti autorizzate/licenziate. Il valore principale è costruire uno storico temporale interrogabile di partite, squadre, classifiche, statistiche e quote, e trasformarlo in dashboard, analytics, backtesting e API.

Repository verificato il 29 settembre 2026: `abderrazak-naceur/Calcio-Analytic`, branch `main`. Al momento contiene solo `README.md`, quindi questo documento definisce la specifica target da cui partire.

## Visione

Flusso target:

`Sources → Ingestion → Raw Data → Normalization → Historical Store → Analytics → Models → Dashboard/API`

Il prodotto non deve essere un semplice clone di un comparatore quote. Deve diventare un data platform che conserva la storia e permette di capire come cambiano mercato, squadre e contesto nel tempo.

## Fonti dati e rischio legale

Oddspedia descrive pubblicamente quote pre-match/live, storico dei movimenti, diversi mercati, statistiche, classifiche e informazioni sulle partite. La loro offerta include anche confronto tra bookmaker e strumenti di analisi.

Per il progetto è fondamentale non assumere che lo scraping massivo e il riutilizzo commerciale dell'intero dataset di un sito terzo siano automaticamente consentiti. La Direttiva 96/9/CE tutela le banche dati e disciplina anche estrazione e riutilizzo dei contenuti.

Decisione architetturale: usare un'astrazione `DataProvider` e supportare API/feed autorizzati, dataset propri e fonti utilizzabili secondo i rispettivi termini. Un eventuale adapter Oddspedia va usato solo quando l'accesso e il riutilizzo sono consentiti.

## Utenti

- Analyst: esplorazione, filtri, grafici e confronti.
- Data enthusiast: storico quote e trend.
- Professional researcher: dataset, API, backtesting e modelli.
- Admin: provider, job, qualità, utenti e piani.
- Business/API customer: integrazione dei dati tramite API.

## Functional Scope

### Competizioni

Paese, lega, competizione, stagione, fase, gruppo, turno e classifiche storiche.

### Squadre

Provider ID, canonical ID, nomi/alias, paese, competizione, stagione, posizione, punti, vittorie, pareggi, sconfitte, goal fatti/subiti, differenza reti, forma, casa e trasferta.

### Giocatori

Quando disponibili e utilizzabili: ID, nome, squadra, ruolo, presenze, minuti, goal, assist, cartellini e stato di disponibilità.

### Partite

Match ID, competition, season, home/away team, kickoff UTC, stadio, arbitro, status, half-time, full-time, extra-time e rigori.

### Quote

Modello:

`Provider → Bookmaker → Match → Market → Selection → OddsSnapshot`

Mercati iniziali: 1X2, Double Chance, Draw No Bet, Asian Handicap, Over/Under, BTTS, Correct Score. Successivi: corner, cards, half-time, HT/FT, player props e live markets.

Ogni snapshot deve avere timestamp UTC, bookmaker, market, selection, quota decimale, provider, batch di ingestion e flag live/pre-match.

### Storico quote

Per ogni selection conservare `timestamp, odd, bookmaker, market, match`.

Calcolare opening, current, closing, min, max, variazione assoluta, variazione percentuale, velocità del movimento, volatilità, estremi temporali e divergenza tra bookmaker.

### Classifiche

Salvare snapshot storici per stagione, competizione, giornata e data. Questo permette analisi coerenti con il momento storico invece di usare soltanto la classifica corrente.

### Statistiche partita

Possesso, tiri, tiri nello specchio, corner, falli, cartellini, fuorigioco, xG quando disponibili e altre metriche supportate dal provider.

## Data Acquisition Architecture

Interfaccia logica:

```text
IDataProvider
  ├── GetCompetitions()
  ├── GetTeams()
  ├── GetStandings()
  ├── GetFixtures()
  ├── GetMatch()
  ├── GetMatchStatistics()
  ├── GetOdds()
  └── GetOddsHistory()
```

Struttura:

```text
/providers
  /official
  /licensed-feed
  /oddspedia
  /custom
```

Ogni provider deve produrre DTO comuni e mantenere la provenienza del dato.

## ETL

### Extract

Scheduler per competition sync, teams sync, fixtures sync, standings sync, statistics sync, odds snapshots, historical repair e reconciliation.

### Raw layer

Conservare il payload originale senza modificarlo:

```text
RawIngestion
- Id
- Provider
- Endpoint
- RequestedAt
- RetrievedAt
- HttpStatus
- Payload
- PayloadHash
- BatchId
- Success
- Error
```

### Normalize

Convertire i dati nel modello canonico. Gestire alias come `Real Madrid`, `Real Madrid CF` e altri nomi equivalenti tramite mapping provider → canonical ID.

### Deduplication

Match key iniziale: `competition + season + kickoff + homeTeam + awayTeam`.

### Validation

Validare timestamp, odds, team, competition, market, bookmaker, score e duplicate snapshots.

## PostgreSQL Architecture

PostgreSQL è il system of record.

Schemi logici:

```text
identity
catalog
matches
odds
statistics
analytics
ingestion
audit
billing
```

Tabelle principali:

```text
catalog
  countries
  competitions
  seasons
  teams
  team_aliases
  players
  bookmakers
  markets
  selections

matches
  matches
  match_events
  match_statistics
  standings_snapshots
  team_form_snapshots

odds
  odds_snapshots
  odds_current
  odds_opening
  odds_closing
  odds_movements

ingestion
  providers
  ingestion_runs
  ingestion_requests
  raw_payloads
  import_errors
  reconciliation_runs

analytics
  feature_snapshots
  team_ratings
  match_probabilities
  model_predictions
  value_observations
  backtest_runs
  backtest_results

billing
  users
  organizations
  subscriptions
  api_keys
  usage_events
```

## Time Series Strategy

Le quote sono dati time-series. MVP: PostgreSQL con indici e partitioning quando il volume cresce. Evoluzione possibile: TimescaleDB se benchmark e carico lo giustificano.

Indici fondamentali:

```text
odds_snapshots(match_id, market_id, selection_id, captured_at)
odds_snapshots(bookmaker_id, captured_at)
matches(competition_id, kickoff_at)
matches(home_team_id, kickoff_at)
matches(away_team_id, kickoff_at)
standings_snapshots(competition_id, season_id, snapshot_at)
```

Retention consigliata: hot 7–30 giorni, warm 12–24 mesi, cold per raw payload archiviati e compressi.

## Analytics Engine

Pipeline:

`Postgres → Analytics Service → Cached Aggregates → Web API → React`

KPI principali: match analizzati, osservazioni quote, bookmaker, competizioni, movimento quote, margine di mercato, hit rate storico, calibrazione, Brier score, log loss e ROI solo su backtest riproducibili.

### Probabilità implicita

Per quota decimale `o`: `p = 1 / o`.

Per mercato con outcome multipli: `overround = Σ(1/o_i)` e `margin = overround - 1`.

Distinguere probabilità implicita grezza da probabilità normalizzata.

### Odds movement

`delta = current - opening`

`deltaPct = ((current - opening) / opening) * 100`

Feature: early movement, late movement, sharp move, reversal, consensus movement e bookmaker divergence.

Un movimento di quota non deve essere presentato come garanzia dell'esito.

## Team Analytics

Forma: W/D/L, punti per partita, goal fatti/subiti, clean sheets.

Casa: punti per partita, GF/GA.

Trasferta: punti per partita, GF/GA.

Rolling windows: ultime 5, ultime 10, stagione, stagione precedente.

Advanced: xG/xGA, shot differential, corner differential, cards e strength of schedule quando disponibili.

## Rating e Models

Possibili modelli:

- ELO.
- Poisson.
- Dixon-Coles.
- Modelli gerarchici bayesiani.
- Machine learning in fase avanzata.

Il framework deve versionare modello, feature set, dataset e timestamp.

## Match Intelligence Page

Header: squadre, kickoff, competizione, status.

Market panel: 1X2, O/U, BTTS, handicap.

Odds timeline: grafico multi-bookmaker.

Team comparison: ranking, forma, casa/trasferta, scoring e conceding.

Historical context: H2H e classifica al momento della partita.

Model panel: probabilities, metriche di calibrazione, model version e intervalli quando appropriato.

## Web Admin / SaaS UI

Tecnologie target: React, TypeScript, Vite, Tailwind CSS, charting library, responsive layout.

Stile: premium SaaS/admin, light/dark mode, command palette `Ctrl/Cmd + K`, tabelle dense, filtri avanzati, skeleton loading e progressive disclosure.

Navigazione proposta:

```text
Dashboard
Matches
  Today
  Upcoming
  Live
  Results
Odds
  Explorer
  Movements
  Best Price
  History
Competitions
Teams
Players
Standings
Analytics
  Market
  Teams
  Form
  Models
  Backtesting
Data
  Providers
  Ingestion Runs
  Errors
  Data Quality
Admin
  Users
  Plans
  Settings
```

Dashboard: Today Matches, Live Matches, Odds Snapshots Today, Active Providers, Data Freshness, Data Quality; grafici per movimenti quote, coverage, distribuzione competizioni e ingestion latency.

## Data Quality Center

Metriche: freshness, missing matches, duplicate rate, normalization confidence, stale bookmakers, ingestion failures, odds anomalies e unexpected score changes.

Visualizzare score e dettaglio delle singole metriche.

## Backend API

Target: ASP.NET Core / .NET 10.

Endpoint:

```text
/api/competitions
/api/seasons
/api/teams
/api/players
/api/matches
/api/matches/{id}
/api/matches/{id}/odds
/api/matches/{id}/statistics
/api/odds
/api/odds/movements
/api/standings
/api/analytics/teams
/api/analytics/markets
/api/analytics/backtests
/api/data-quality
/api/ingestion/runs
```

## Background Workers

.NET Worker Services + Hangfire o scheduler equivalente + PostgreSQL + Redis.

Frequenze indicative, subordinate ai rate limit del provider:

```text
Every 5-15 min: upcoming odds snapshots
Every 1-5 min: live data quando previsto
Every few hours: reconciliation
Daily: standings, features, quality checks
Weekly: aggregates, cleanup, backtests
```

## Caching

Redis per current odds, today's matches, standings, dashboard KPIs e pagine match popolari.

TTL più breve per live data, più lungo per standings e historical analytics.

## Security

OAuth/OIDC, JWT/API keys, RBAC, rate limiting, tenant isolation, audit log, secrets manager, TLS e database least privilege.

Ruoli: Admin, Analyst, Premium User, API Customer, Viewer.

## Multi-tenancy

`Organization → Users → Subscription → Entitlements`

I dati privati usano `tenant_id`; saved filters, private models, private backtests, private datasets e API usage sono tenant-scoped.

## Backtesting

Regola principale: **nessun look-ahead bias**.

Una simulazione deve usare solo dati disponibili nello stesso punto temporale della storia. Salvare model version, feature version, dataset version, timestamp, prediction, market price, settlement e result.

Metriche: ROI, yield, hit rate, Brier score, log loss, calibration, maximum drawdown e sample size.

I risultati storici non garantiscono risultati futuri.

## AI Analytics

Fase avanzata: assistente linguistico sopra il data warehouse.

Architettura:

```text
IAssistant
  ├── Query Planner
  ├── Metrics Resolver
  ├── SQL/Analytics Executor
  ├── Explanation Layer
  └── Data Provenance
```

Esempio: `Mostrami le partite di Serie A di oggi in cui la quota 1X2 ha subito una variazione superiore al 10% nelle ultime 6 ore.`

L'AI deve trasformare la richiesta in filtri/query strutturati e non inventare dati. Ogni risposta deve indicare dataset, finestra temporale, timestamp, filtri, freshness e model version se pertinente.

## API / Export

Piani superiori possono offrire CSV, JSON, Excel, PDF e API REST con pagination, filtering, sorting, date ranges, API keys e usage quotas.

## Business Model

Possibile modello SaaS da validare:

```text
Free       €0
Pro        €19–29/mese
Analyst    €49–79/mese
Business   €149–299/mese
Enterprise custom
```

Questi prezzi sono ipotesi iniziali, non dati di mercato verificati.

Revenue streams: subscription, API usage, dataset licensing quando legalmente disponibile, enterprise integrations, custom analytics, white-label dashboards e research reports.

Principali costi: data feeds/licenze, PostgreSQL, Redis, object storage, workers, monitoring, CDN, autenticazione, email, payment processing, support e marketing.

## MVP

### Data

3–5 competizioni principali, matches, standings, teams, 1X2, O/U, BTTS, bookmakers e historical snapshots.

### Backend

Provider adapter, ingestion engine, raw layer, PostgreSQL, normalized domain e REST API.

### Frontend

Dashboard, Match List, Match Detail, Odds Explorer, Teams, Standings e Data Quality.

### Analytics

Implied probability, overround, movement, basic team form e historical charts.

## Phase 2

Asian handicap, corners, cards, player data, xG, advanced ratings, ELO, Poisson, backtesting, alerts e saved filters.

## Phase 3

Machine learning, model registry, feature store, calibration avanzata, recommendation engine, natural language analytics e AI assistant.

## Repository Structure

```text
Calcio-Analytic/
├── docs/
│   ├── PRODUCT-AND-BUSINESS-PLAN.md
│   ├── ARCHITECTURE.md
│   ├── DATA-MODEL.md
│   ├── DATA-PROVIDERS.md
│   ├── ANALYTICS.md
│   ├── API.md
│   ├── BUSINESS-PLAN.md
│   ├── ROADMAP.md
│   └── SECURITY.md
├── src/
│   ├── CalcioAnalytic.Api/
│   ├── CalcioAnalytic.Application/
│   ├── CalcioAnalytic.Domain/
│   ├── CalcioAnalytic.Infrastructure/
│   ├── CalcioAnalytic.Ingestion/
│   ├── CalcioAnalytic.Analytics/
│   └── CalcioAnalytic.Worker/
├── web/
│   └── calcio-analytic-web/
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   └── DataQualityTests/
└── infra/
    ├── docker/
    └── migrations/
```

## Roadmap

### Sprint 0 — Foundation
Solution, CI, Docker, PostgreSQL, Redis, auth e migrations.

### Sprint 1 — Catalog
Competitions, seasons, teams, bookmakers e markets.

### Sprint 2 — Matches
Fixtures, results e match details.

### Sprint 3 — Odds ingestion
Provider adapter, raw storage, normalization e snapshots.

### Sprint 4 — History
Time-series queries, movement engine e historical charts.

### Sprint 5 — Frontend
Dashboard, match explorer e odds explorer.

### Sprint 6 — Team analytics
Form, standings e home/away.

### Sprint 7 — Backtesting
Model framework, historical replay e metrics.

### Sprint 8 — SaaS
Plans, billing, quotas e API keys.

## Product KPIs

Acquisition: signups, activation.

Engagement: DAU/WAU, searches, match pages, saved filters.

Conversion: free-to-paid, trial-to-paid.

Retention: D7, D30, churn.

Data: freshness, coverage, failed runs.

Technical: API p95, ingestion throughput, error rate, cost per active user.

## Product Risks

### Data licensing
Mitigation: provider abstraction, contratti/licenze, provenance.

### Data quality
Mitigation: raw storage, reconciliation, confidence score.

### Storage growth
Mitigation: partitioning, compression, retention, aggregates.

### Model risk
Mitigation: walk-forward backtesting, calibration, model versioning, no look-ahead.

### Regulatory/commercial
Mitigation: legal review, terms review e posizionamento responsabile.

## Definition of Done — MVP

- provider autorizzato configurabile;
- PostgreSQL;
- idempotent ingestion;
- raw payload storage;
- normalized entities;
- matches;
- teams;
- standings;
- bookmakers;
- markets;
- odds snapshots;
- odds history;
- REST API;
- Dashboard;
- Match detail;
- Odds Explorer;
- Team page;
- Data Quality;
- authentication;
- Docker;
- CI;
- integration tests;
- audit/provenance;
- documentation.

## Priorità di implementazione

1. Architecture
2. PostgreSQL schema
3. Ingestion abstraction
4. Provider contract
5. Match + team + competition
6. Odds snapshots
7. Historical movement
8. Analytics API
9. Dashboard
10. Backtesting
11. SaaS/billing

Il vantaggio competitivo è il **data layer storico**, quindi la prima implementazione deve privilegiare qualità, storicizzazione e provenance rispetto alla sola UI.

## Fonti

- Oddspedia, quote calcio: https://oddspedia.com/it/calcio/quote
- Oddspedia, comparazione quote: https://oddspedia.com/it/quote
- Oddspedia, football data/statistiche: https://oddspedia.com/football
- Oddspedia widgets/data capabilities: https://widgets.oddspedia.com/it
- EUR-Lex, Direttiva 96/9/CE: https://eur-lex.europa.eu/legal-content/it/TXT/?uri=CELEX:31996L0009

> Nota: le feature dipendenti dal provider sono condizionate da disponibilità, licenza e qualità della fonte. Questo documento è una specifica di prodotto/architettura e non un parere legale.