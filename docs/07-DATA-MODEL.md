# 07 — Data Model

## Core entities
Country, Competition, Season, Team, TeamAlias, Player, Bookmaker, Market, Selection, Match, MatchStatistic, StandingSnapshot, OddsSnapshot, OddsMovement, IngestionRun, RawPayload, FeatureSnapshot, ModelPrediction, BacktestRun.

## Relationships
Competition → Season → Match.
Team → Match home/away.
Match → Market → Selection → OddsSnapshot.
Competition + Season → StandingSnapshot.
Provider → RawPayload → Canonical entity.
FeatureSnapshot → ModelPrediction → BacktestResult.

## Identity
External provider IDs non sono la business identity. Usare internal UUID/ULID e mapping tables.

## Temporal
I record storici sono append-oriented. Non sovrascrivere uno snapshot storico con il dato corrente.
