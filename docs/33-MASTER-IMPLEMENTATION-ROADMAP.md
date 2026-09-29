# 33 — Master Implementation Roadmap

## 0. Development rule

Development is sequential. Do not start a later phase by bypassing an incomplete foundation.

Each phase has:
- objective
- implementation tasks
- tests
- documentation
- acceptance criteria

A phase is DONE only when its acceptance criteria are verified.

---

# PHASE 1 — Repository and development foundation

## Objective
Create a clean, reproducible development environment.

## Tasks
- Create solution structure.
- Create .NET 10 projects.
- Create React + TypeScript + Vite application.
- Create Python/FastAPI service.
- Create Docker Compose.
- Add PostgreSQL.
- Add Redis.
- Add environment configuration.
- Add .gitignore and editor configuration.
- Add local development README.
- Add health endpoints.
- Add GitHub Actions CI skeleton.

## Output
The repository starts all services locally.

## Acceptance
- dotnet build succeeds.
- frontend installs/builds.
- Python service starts.
- PostgreSQL starts.
- Redis starts.
- CI runs successfully.

---

# PHASE 2 — Backend architecture

## Objective
Establish clean .NET boundaries before implementing domain logic.

## Projects
- CalcioAnalytic.Api
- CalcioAnalytic.Application
- CalcioAnalytic.Domain
- CalcioAnalytic.Infrastructure
- CalcioAnalytic.Contracts
- CalcioAnalytic.Ingestion
- CalcioAnalytic.Analytics
- CalcioAnalytic.Workers

## Tasks
- Dependency rules.
- DI setup.
- configuration options.
- logging.
- error handling.
- validation.
- API versioning.
- correlation IDs.
- health checks.

## Acceptance
No circular dependencies and no provider DTOs in Domain.

---

# PHASE 3 — PostgreSQL foundation

## Objective
Create the system of record.

## Schemas
- identity
- catalog
- matches
- odds
- statistics
- analytics
- ingestion
- audit

## First entities
- Provider
- Country
- Competition
- Season
- Team
- Player
- Bookmaker
- Market
- Match

## Tasks
- EF Core configuration.
- migrations.
- indexes.
- unique constraints.
- foreign keys.
- UTC timestamps.
- soft deletion only where justified.

## Acceptance
Database can be created from zero using migrations.

---

# PHASE 4 — Provider abstraction

## Objective
Prevent the application from depending on one data vendor.

## Contracts
- IFootballProvider
- IFixtureProvider
- IOddsProvider
- IStatisticsProvider
- IStandingsProvider
- IBookmakerProvider

## Canonical DTOs
- ProviderMatchDto
- ProviderTeamDto
- ProviderBookmakerDto
- ProviderMarketDto
- ProviderOddsSnapshotDto
- ProviderEventDto
- ProviderStatisticDto

## Tasks
- provider registry.
- provider configuration.
- provider identity mapping.
- adapter error handling.
- raw payload provenance.

## Acceptance
A second provider can be added without changing Domain entities.

---

# PHASE 5 — Football catalog ingestion

## Objective
Build the football universe.

## Collect
- countries
- competitions
- seasons
- teams
- players
- bookmakers
- markets

## Tasks
- scheduled synchronization.
- mapping tables.
- aliases.
- deduplication.
- reconciliation.

## Acceptance
Catalog contains stable internal IDs and provider IDs.

---

# PHASE 6 — Match ingestion

## Objective
Create the canonical match lifecycle.

## States
Scheduled
-> PreMatch
-> Live
-> Finished
-> SettlementPending
-> Analyzed
-> Reconciled

## Collect
- fixture
- kickoff
- venue
- teams
- competition
- round
- referee when available
- status
- score

## Acceptance
A provider fixture becomes one canonical Match and can be updated idempotently.

---

# PHASE 7 — Odds ingestion engine

## Objective
Build the most important historical data layer.

## Model
Provider
-> Bookmaker
-> Match
-> Market
-> Line
-> Selection
-> OddsSnapshot

## Store
- decimal odds
- fractional odds when available
- American odds when available
- implied probability
- bookmaker timestamp
- provider timestamp
- ingestion timestamp
- live/pre-match flag
- period
- line
- match minute
- payload hash

## Rules
Never overwrite historical snapshots.

## Tasks
- current odds projection.
- opening odds.
- closing odds.
- snapshot deduplication.
- market mapping.
- bookmaker mapping.
- suspended selections.
- odds movement detection.

## Acceptance
A sequence of price changes is completely reproducible.

---

# PHASE 8 — Match statistics and events

## Objective
Collect the actual football context.

## Events
- goals
- cards
- substitutions
- VAR events
- penalties
- corners when available

## Statistics
- possession
- shots
- shots on target
- passes
- fouls
- corners
- cards
- xG where licensed/available
- other provider metrics

## Historical context
- standings at match time
- team form at match time
- H2H at match time

## Acceptance
Historical context is point-in-time correct.

---

# PHASE 9 — Result reconciliation and settlement

## Objective
Close a match correctly.

## Tasks
- final score reconciliation.
- half-time score.
- extra time handling.
- penalty shootout handling.
- match status confirmation.
- closing odds selection.
- market settlement.

## Settlement statuses
- WON
- LOST
- VOID
- PUSH
- UNKNOWN

Never guess an unsupported settlement.

## Acceptance
The same finished match can be processed repeatedly without changing the outcome.

---

# PHASE 10 — MatchAnalysisEngine

## Objective
Generate the first complete analytical report.

## Analysis
- result summary.
- opening odds.
- closing odds.
- average odds.
- best/worst bookmaker price.
- odds movement.
- movement percentage.
- number of changes.
- volatility.
- overround.
- implied probability.
- normalized probability.
- bookmaker dispersion.
- market settlement.
- team context.
- statistics summary.

## Output
Immutable MatchAnalysis version.

## Acceptance
A finished match automatically receives an analysis.

---

# PHASE 11 — Historical Pattern Engine

## Objective
Allow users to ask historical questions.

## Filters
- competition
- season
- date range
- home/away
- bookmaker
- market
- opening odds
- closing odds
- movement
- line
- team strength
- form
- standings context

## Output
- sample size
- result distribution
- market distribution
- odds distribution
- movement distribution
- bookmaker distribution
- confidence intervals where appropriate
- data completeness
- methodology
- analysis version

## Acceptance
Every query is reproducible from a stored query definition and dataset version.

---

# PHASE 12 — Similar Match Engine

## Objective
Find historically comparable matches.

## Features
- competition
- home/away
- team ratings
- rating difference
- standings
- form
- goals averages
- H2H
- opening odds
- closing odds
- movement
- market lines

## Tasks
- feature vector.
- normalization.
- similarity method.
- top-K retrieval.
- explanation of similarity.

## Acceptance
Results use only information available at the historical cutoff.

---

# PHASE 13 — Python quantitative layer

## Objective
Move advanced statistical workloads into Python.

## Services
- FastAPI
- match analysis
- pattern analysis
- similarity
- backtesting
- model evaluation

## Libraries
- Pandas
- NumPy
- SciPy
- statsmodels
- scikit-learn

Optional after profiling:
- XGBoost
- Polars
- PyArrow

## Acceptance
Python can process a controlled analytical dataset and return versioned results.

---

# PHASE 14 — Models

## Objective
Introduce statistical baselines before advanced ML.

## Models
1. Implied bookmaker probabilities.
2. ELO.
3. Poisson.
4. Dixon-Coles.
5. ML baseline.

## Evaluation
- log loss
- Brier score
- calibration
- accuracy where meaningful
- MAE/RMSE for numeric predictions

## Rule
Models are evaluated against historical data; they are not presented as guarantees.

## Acceptance
Every model run is reproducible.

---

# PHASE 15 — Backtesting Engine

## Objective
Test analytical methods without future-data leakage.

## Required
- dataset version
- cutoff timestamp
- feature version
- model version
- parameters
- predictions
- actual results
- metrics

## Anti-leakage
Never use:
- future result
- future odds
- future standings
- future form
- future statistics
- closing price when simulating an earlier timestamp

## Acceptance
A backtest can be rerun and produce the same result under the same version/seed.

---

# PHASE 16 — .NET Analytics API

## Objective
Expose analytics to the web application.

## Endpoints
- GET /api/v1/analytics/matches/{id}
- GET /api/v1/analytics/matches/{id}/odds
- GET /api/v1/analytics/matches/{id}/movement
- GET /api/v1/analytics/matches/{id}/bookmakers
- GET /api/v1/analytics/matches/{id}/similar
- POST /api/v1/analytics/patterns/query
- POST /api/v1/analytics/backtests
- GET /api/v1/analytics/backtests/{id}

## Documentation
OpenAPI + Swagger.

## Acceptance
All endpoints are documented and have automated contract/integration tests.

---

# PHASE 17 — Frontend foundation

## Objective
Create the analytics workspace.

## Stack
React + TypeScript + Vite + Tailwind.

Vite officially provides a React TypeScript template. citeturn0search7

## Tasks
- routing.
- API client.
- authentication shell.
- layout.
- navigation.
- theme.
- table components.
- chart components.
- filters.
- loading states.
- error states.

## Acceptance
Frontend consumes the real API and contains no fake production data.

---

# PHASE 18 — Match Detail UI

## Objective
Build the main analytical screen.

## Tabs
- Overview
- Odds
- Odds Movement
- Bookmakers
- Markets
- Events
- Statistics
- Form
- Standings
- H2H
- Similar Matches
- Historical Patterns
- Models
- Data Quality

## Main visualizations
- odds timeline.
- bookmaker matrix.
- result summary.
- statistics comparison.
- form timeline.
- historical distribution.

## Acceptance
A finished match can be understood from one screen without opening database tools.

---

# PHASE 19 — Historical Analytics UI

## Objective
Make the historical dataset searchable.

## Screens
- Pattern Explorer.
- Market Explorer.
- Bookmaker Explorer.
- Similar Matches.
- Odds Movement Explorer.
- Result Explorer.

## Requirements
- saved filters.
- URL-shareable query.
- export.
- sample size.
- methodology.
- data quality.
- date range.

## Acceptance
A user can reproduce a historical analysis from the UI.

---

# PHASE 20 — Dashboard

## Objective
Create the main Calcio-Analytic command center.

## KPIs
- matches collected
- matches analyzed
- odds snapshots
- bookmakers
- markets
- competitions
- data freshness
- ingestion health
- analysis backlog

## Views
- today's matches
- live matches
- recent results
- biggest odds movements
- market activity
- data quality
- provider health

## Acceptance
Dashboard uses optimized read models rather than expensive raw joins.

---

# PHASE 21 — Data quality and reconciliation

## Objective
Make the platform trustworthy.

## Checks
- missing mappings
- duplicate matches
- duplicate odds
- impossible odds
- stale data
- provider conflicts
- missing results
- incomplete markets
- invalid timestamps
- impossible event sequences

## Output
Data quality score per:
- provider
- competition
- match
- bookmaker
- market
- day

## Acceptance
Incomplete data is visible and never silently treated as complete.

---

# PHASE 22 — Performance and scale

## Objective
Prepare for millions of matches and large odds history.

## Tasks
- query profiling.
- indexes.
- materialized/read models.
- partitioning for large tables.
- retention strategy.
- object storage for raw payloads.
- batch ingestion.
- queue scaling.

PostgreSQL supports declarative partitioning and partition pruning; partitioning should be introduced when measured data volume justifies it, not prematurely. citeturn0search0

## Acceptance
Load tests identify stable throughput and query latency targets.

---

# PHASE 23 — Observability and operations

## Objective
Operate the platform reliably.

## Metrics
- provider latency
- provider errors
- ingestion lag
- odds freshness
- analysis backlog
- Python job failures
- database size
- queue depth
- API latency

## Tracing
OpenTelemetry across:
Provider -> .NET -> Worker -> Python -> PostgreSQL -> API.

## Acceptance
A failed analysis can be traced from API/job to provider payload.

---

# PHASE 24 — Security

## Objective
Protect data and services.

## Tasks
- authentication.
- authorization.
- API keys.
- secrets management.
- rate limiting.
- CORS.
- security headers.
- audit logs.
- internal Python authentication.
- provider credential isolation.

## Acceptance
No secret is committed or exposed to browser code.

---

# PHASE 25 — Provider expansion

## Objective
Increase data coverage.

## Strategy
Provider adapter per source.

Candidate categories:
- football data provider
- odds provider
- odds comparison provider
- statistics provider
- licensed historical feed

Do not couple the product to one provider.

## Acceptance
Provider failure does not destroy the application; coverage degrades transparently.

---

# PHASE 26 — Historical data backfill

## Objective
Build the historical dataset required for meaningful analysis.

## Tasks
- select licensed historical sources.
- import seasons.
- import fixtures/results.
- import statistics.
- import historical odds.
- normalize identities.
- reconcile.
- validate completeness.

## Acceptance
Historical data has documented coverage and provenance.

---

# PHASE 27 — SaaS layer

## Objective
Turn the platform into a multi-user product.

## Features
- organizations
- users
- roles
- subscriptions
- API keys
- quotas
- usage
- saved analyses
- saved filters
- exports

## Acceptance
Users can only access authorized data and resources.

---

# PHASE 28 — AI analytics assistant

## Objective
Add natural-language access to the analytical dataset.

## Examples
- "Analizza questa partita."
- "Confronta il movimento delle quote."
- "Trova partite storiche simili."
- "Mostrami come è cambiata la quota."
- "Spiegami perché il report contiene dati incompleti."

## Rule
AI summarizes stored analytical facts. It must not invent missing statistics or present historical patterns as guaranteed predictions.

## Acceptance
Every AI statement can be traced to underlying analytical data.

---

# PHASE 29 — Production deployment

## Components
- frontend
- .NET API
- Python analytics
- workers
- PostgreSQL
- Redis
- object storage
- monitoring

## Tasks
- production Docker images.
- secrets.
- migrations.
- backups.
- health checks.
- rolling deployment.
- logging.
- alerting.
- disaster recovery.

## Acceptance
A clean environment can be deployed from documented instructions.

---

# PHASE 30 — Final validation

## Functional
- catalog ingestion
- match ingestion
- odds history
- live updates
- result reconciliation
- settlement
- analysis
- historical patterns
- similarity
- models
- backtesting
- UI
- API
- authentication

## Data
- provenance
- timestamps
- point-in-time correctness
- duplicate protection
- completeness
- reproducibility

## Performance
- ingestion throughput
- API latency
- historical query latency
- analysis latency
- database growth

## Security
- secrets
- authorization
- rate limits
- audit
- internal service isolation

## Acceptance
The platform can ingest a match, preserve its complete history, process its final result, generate a reproducible analysis and display it through the web application.

---

# Recommended execution order

Do NOT implement everything simultaneously.

Execute:

1. Foundation
2. .NET architecture
3. PostgreSQL
4. Provider abstraction
5. Catalog
6. Matches
7. Odds
8. Statistics/events
9. Results/settlement
10. MatchAnalysisEngine
11. HistoricalPatternEngine
12. SimilarMatchEngine
13. Python quantitative layer
14. Models
15. Backtesting
16. Analytics API
17. React/Vite foundation
18. Match Detail
19. Historical UI
20. Dashboard
21. Data quality
22. Scale
23. Observability
24. Security
25. Provider expansion
26. Historical backfill
27. SaaS
28. AI
29. Production
30. Final validation

# First milestone

The first real milestone is not the dashboard.

It is:

**One real match -> provider data -> PostgreSQL -> complete odds snapshots -> final result -> automatic MatchAnalysis -> API -> web Match Detail.**

Once this vertical slice works end-to-end, expand horizontally.

# Definition of DONE

A phase is DONE only if:
- code exists
- tests exist
- documentation is updated
- migrations are reproducible
- logs/metrics exist where required
- historical correctness is verified
- no known critical data-loss issue remains
- acceptance criteria pass

# Development principle

Build the smallest complete vertical slice first, then expand coverage.

Never build UI screens on fake analytical assumptions. The data model and analytical contracts must exist before the corresponding production UI is considered complete.
