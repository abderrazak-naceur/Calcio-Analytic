# 23 — Observability

## Goals
Make ingestion, analysis and provider health measurable and diagnosable.

## Metrics

### Ingestion
- ingestion_runs_total
- ingestion_errors_total
- provider_requests_total
- provider_request_latency_ms
- provider_rate_limits_total
- payloads_received_total
- records_normalized_total
- records_rejected_total

### Odds
- odds_snapshots_total
- odds_changes_total
- bookmakers_seen
- markets_seen
- stale_odds_total
- odds_mapping_failures_total

### Analysis
- post_match_analysis_total
- analysis_duration_ms
- analysis_failures_total
- similar_match_queries_total
- pattern_queries_total
- backtest_runs_total

### Data quality
- missing_match_fields
- missing_odds_fields
- duplicate_records
- reconciliation_failures
- provider_conflicts

## Structured logs
Every job log should include:
- correlationId
- provider
- providerRequestId when available
- matchId
- providerMatchId
- jobName
- attempt
- durationMs
- recordsRead
- recordsWritten
- errors

Never log API secrets or personal data.

## Tracing
Use OpenTelemetry across:
Provider -> Adapter -> Normalizer -> Repository -> Analytics -> API.

## Alerts
Alert on:
- provider outage
- elevated provider latency
- ingestion failure rate
- stale odds
- mapping failure spike
- analysis backlog
- database storage pressure
- failed scheduled jobs

## Dashboards
Operations dashboard:
- provider health
- ingestion lag
- queue depth
- failures

Data dashboard:
- completeness
- freshness
- reconciliation
- bookmaker/market coverage

Analytics dashboard:
- analyses generated
- historical query latency
- backtest volume
- model evaluation status

### Local dashboard stack

The Docker Compose stack now includes an OpenTelemetry Collector, Prometheus and
Grafana. The API exports OTLP telemetry to the collector when Compose is used;
Prometheus receives the collector's metrics and Grafana is provisioned with the
`Calcio-Analytic Operations Overview` dashboard.

- Grafana: `http://localhost:3001`
- Prometheus: `http://localhost:9090`
- OTLP gRPC: `localhost:4317`
- OTLP HTTP: `localhost:4318`

The dashboard intentionally uses only telemetry emitted by the application
(ASP.NET Core HTTP instrumentation and .NET runtime metrics). Domain-specific
ingestion/provider counters should be added when their instrumentation exists;
the dashboard must not manufacture operational data.

## Auditability
Every generated analysis stores:
- analysis version
- feature version
- provider data versions
- generatedAt
- source snapshot identifiers
This allows a report to be reproduced later.
