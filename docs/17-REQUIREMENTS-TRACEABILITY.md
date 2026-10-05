# 17 — Requirements Traceability

| Requirement | Module | Test | Priority |
|---|---|---|---|
| Provider ingestion | Ingestion | Integration | P0 |
| Raw payload storage | Ingestion | Integration | P0 |
| Team normalization | Catalog | Unit/Integration | P0 |
| Match lifecycle | Matches | Integration | P0 |
| Odds snapshots | Odds | Integration | P0 |
| Odds history | Analytics | Integration | P0 |
| Standings history | Matches | Integration | P0 |
| Dashboard | Web | E2E | P0 |
| Data quality | Operations | Integration | P0 |
| Backtesting | Analytics | Data/Integration | P1 |
| API customers | API | Integration | P1 |
| AI analytics | AI | E2E | P2 |
| Market outcome classification | Analytics | Unit/Integration | P1 |
| Favorite failure / upset analysis | Analytics API + Web | Integration/E2E | P1 |
| Market Failures / Match Detail | Web | E2E | P1 |
| SportMonks odds ingestion | Ingestion | Integration | P0 |

Ogni requisito P0 deve avere implementazione e test automatici prima del rilascio MVP.

## Current Market Intelligence traceability

| Requirement | Current state |
|---|---|
| Favorite detection | Implemented and covered by deterministic tests |
| HIT / MISS | Implemented and covered by deterministic tests |
| UPSET | Implemented with configurable threshold and deterministic tests |
| BTTS settlement | Implemented with representative fixture/tests |
| Market Outcome API | Implemented for 1X2, OU and BTTS |
| Dashboard Market vs Reality | Implemented |
| Market Failures view | Implemented |
| Match Detail Market Reality | Implemented |
| Real SportMonks validation | Pending |
| Historical backfill / full statistical depth | Pending |
