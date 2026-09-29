# 28 — .NET API and Swagger

## Base
All customer endpoints use `/api/v1`.

OpenAPI document:
`/openapi/v1.json`

Swagger UI:
`/swagger`

.NET 10 supports OpenAPI 3.1 generation through Microsoft.AspNetCore.OpenApi. Swagger UI can be layered on top for interactive testing. citeturn0search4turn0search7

## Endpoint groups
- Catalog: countries, competitions, seasons, teams, players, bookmakers, markets.
- Matches: match, events, statistics, standings, form, H2H.
- Odds: current, history, opening, closing, movement.
- Analytics: match analysis, similar matches, historical patterns, backtests, dashboard.
- Admin: providers, ingestion runs, data quality, analytics jobs.

## Required OpenAPI metadata
Every endpoint documents operationId, parameters, request/response schemas, errors, authorization and examples.

## Error contract
```json
{"error":{"code":"DATA_INCOMPLETE","message":"Historical odds are incomplete.","requestId":"uuid","details":{}}}
```

## Security
Production Swagger is protected or disabled. Admin APIs are never anonymously exposed.

## Client generation
Generate typed TypeScript API clients from OpenAPI where practical. The OpenAPI contract remains the source of truth.
