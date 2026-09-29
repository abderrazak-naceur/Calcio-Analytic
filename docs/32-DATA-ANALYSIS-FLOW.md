# 32 — End-to-End Data and Analysis Flow

## Lifecycle
Discovery -> Pre-match -> Live -> Finished -> Settlement -> Analysis -> Historical Aggregation -> UI.

## Pre-match
Collect fixture, standings, form, H2H, statistics, bookmakers, markets and odds. Every odds change becomes a historical snapshot.

## Live
Collect score, events, statistics and live odds with separate timestamps for bookmaker update, provider update and ingestion.

## Finish
1. Reconcile final result.
2. Retrieve final statistics.
3. Identify closing odds.
4. Settle supported markets.
5. Freeze analytical inputs.

## Analysis
Calculate:
- opening/closing odds
- movement
- implied probability
- overround
- bookmaker dispersion
- market settlement
- team/context analysis
- historical comparison
- similar matches
- model evaluation when requested

## Persistence
Store analysisVersion, featureVersion, datasetVersion, sourceSnapshotIds, generatedAt, calculations, warnings and completeness.

## Aggregation
Update competition, bookmaker, market, historical-distribution, similarity and dashboard read models.

## UI
React reads precomputed analytical views from .NET. Critical historical calculations are not performed in the browser.

## Reproducibility
An analysis must be reproducible from stored snapshot IDs and version metadata. Algorithm changes create a new analysis version rather than silently rewriting history.
