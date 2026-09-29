# 26 — Analytics Engine

## 1. Purpose

Calcio-Analytic is a football data and historical analytics platform. The Analytics Engine converts raw and normalized football data into reproducible match reports, odds analysis, bookmaker comparisons, historical patterns and backtests.

The engine is descriptive and statistical. It must never present a historical correlation as a guaranteed future outcome.

## 2. Main pipeline

Provider data
-> Raw payload
-> Canonical model
-> PostgreSQL
-> Feature snapshots
-> Match lifecycle
-> PostMatchAnalysis
-> Historical aggregates
-> API/read models
-> Web dashboard

## 3. Match lifecycle

Scheduled
-> PreMatch
-> Live
-> Finished
-> SettlementPending
-> Analyzed
-> Reconciled

A match can be reprocessed safely. Analysis versions must be immutable.

## 4. Match analysis

For every finished match generate:

### Identity
- competition
- season
- round
- kickoff
- venue
- home team
- away team
- provider mappings

### Result
- final score
- half-time score
- extra time when applicable
- penalties when applicable
- winner/draw classification

### Team context
- standings position at kickoff
- points at kickoff
- recent form
- goals for/against
- home/away splits
- H2H snapshot
- ratings available at the analysis timestamp

### Match statistics
Store every provider statistic with:
- provider
- canonical metric
- value
- period
- capturedAt
- confidence/completeness

## 5. Odds analysis

Canonical hierarchy:

Provider
-> Bookmaker
-> Match
-> Market
-> Line
-> Selection
-> Snapshot

Never model markets as one fixed set of database columns.

### Snapshot
Required fields:
- matchId
- providerId
- bookmakerId
- marketId
- marketLineId
- selectionId
- decimalOdds
- impliedProbability
- bookmakerUpdatedAt
- providerUpdatedAt
- ingestedAt
- isLive
- period
- matchMinute
- payloadHash

### Derived metrics

Implied probability:
p = 1 / decimalOdds

Overround:
overround = sum(p_i)

Normalized probability:
p_normalized = p_i / sum(p_i)

Movement:
delta = closingOdds - openingOdds

Movement percentage:
deltaPct = ((closingOdds - openingOdds) / openingOdds) * 100

Also calculate:
- minimum odds
- maximum odds
- number of changes
- first movement timestamp
- last movement timestamp
- movement velocity
- reversal count
- volatility
- bookmaker dispersion
- consensus price where supported

All metrics retain the underlying snapshot IDs used to calculate them.

## 6. Bookmaker analysis

For each bookmaker and market:
- opening price
- best price
- worst price
- closing price
- number of updates
- time of first update
- time of last update
- suspension periods
- difference versus market consensus
- difference versus other bookmakers

Never infer bookmaker intent.

## 7. Market settlement

Settlement is calculated only when the necessary final data exists.

Examples:
- 1X2 from final result
- Over/Under from goals
- BTTS from both teams scoring
- Correct Score from final score
- Half-time markets from half-time result
- Corners from final corner count
- Cards from canonical card rules
- Player markets from verified player statistics

If settlement cannot be determined reliably:
status = UNKNOWN

Never guess.

## 8. Historical Pattern Engine

Users can query historical conditions such as:

openingOdds between A and B
AND movementPct between C and D
AND competition = X
AND market = Y
AND date range = Z

The engine returns:
- matching sample count
- outcome counts
- outcome percentages
- odds distributions
- opening/closing distributions
- movement distributions
- bookmaker distributions
- confidence intervals where statistically appropriate
- data completeness
- query definition
- analysis version

## 9. Similar Match Engine

Similarity features may include:
- competition
- season
- home/away
- team strength
- rating difference
- standings difference
- recent form
- goals averages
- H2H features
- opening odds
- closing odds
- odds movement
- market lines
- available pre-match statistics

Similarity must use only features available at the requested historical timestamp.

## 10. Model evaluation

The engine can evaluate:
- implied probabilities
- normalized bookmaker probabilities
- ELO
- Poisson
- Dixon-Coles
- ML models

Metrics:
- log loss
- Brier score
- calibration error
- accuracy where appropriate
- MAE/RMSE for numeric predictions

For probability models, calibration is more important than raw accuracy.

## 11. Backtesting

Every backtest stores:
- dataset version
- cutoff timestamp
- feature version
- model version
- parameters
- included matches
- excluded matches
- predictions
- actual results
- metrics

Rules:
- no future leakage
- no future standings
- no future form
- no closing odds when simulating an earlier timestamp
- no result information in features
- reproducible random seeds where applicable

## 12. Daily processing

A scheduled worker processes finished matches:

1. discover finished matches
2. reconcile result
3. finalize statistics
4. finalize closing line
5. settle supported markets
6. calculate odds movement
7. generate MatchAnalysis
8. update aggregates
9. update similarity index
10. mark reconciled

## 13. Read models

Create optimized read models:
- MatchAnalysisView
- OddsMovementView
- BookmakerComparisonView
- MarketResultSummaryView
- HistoricalPatternResultView
- SimilarMatchView
- AnalyticsDashboardView

The UI should not execute dozens of joins for every screen.

## 14. APIs

Core:
GET /api/v1/analytics/matches/{matchId}
GET /api/v1/analytics/matches/{matchId}/odds
GET /api/v1/analytics/matches/{matchId}/movement
GET /api/v1/analytics/matches/{matchId}/bookmakers
GET /api/v1/analytics/matches/{matchId}/similar
POST /api/v1/analytics/patterns/query
POST /api/v1/analytics/backtests

## 15. Storage

Large historical tables should be partitioned by time, normally by month or another measured retention boundary.

Raw provider payloads should be immutable. Store references/hashes in PostgreSQL and move large payload bodies to object storage when volume requires it.

## 16. Versioning

Every analysis record has:
- analysisVersion
- generatedAt
- featureVersion
- modelVersion when applicable
- sourceSnapshotIds

A change to calculation logic creates a new analysis version rather than silently rewriting historical results.

## 17. UI output

The match page should contain:
Overview
Odds
Odds Movement
Bookmakers
Markets
Statistics
Events
Form
Standings
H2H
Similar Matches
Historical Patterns
Models
Data Quality

The most important visualizations are:
- odds movement timeline
- bookmaker matrix
- market distribution
- result distribution
- historical comparison
- team form
- match statistics

## 18. Data quality

Every report exposes:
- completeness
- freshness
- provider coverage
- mapping status
- unresolved fields
- settlement status

A visually impressive report is invalid if its source data is incomplete without saying so.
