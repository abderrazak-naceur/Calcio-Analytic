# 05 — Data Architecture

## PostgreSQL schemas
identity, catalog, matches, odds, statistics, analytics, ingestion, audit, billing.

## Storage layers
1. Raw: payload provider immutabile.
2. Canonical: entità normalizzate.
3. Feature: feature analitiche.
4. Aggregate: dati ottimizzati per dashboard/query.

## Time series
Odds snapshots e standings storiche sono dati temporali di prima classe.

## Retention
Hot 7–30 giorni, warm 12–24 mesi, cold archive secondo piano, costo e licenza.

## Indexes
odds_snapshots su match/market/selection/captured_at e bookmaker/captured_at; matches su competition/kickoff e home/away/kickoff; standings su competition/season/snapshot.

## Partitioning
Partitioning mensile delle quote quando i benchmark lo giustificano.
