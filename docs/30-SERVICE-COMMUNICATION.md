# 30 — Service Communication

## Synchronous path
Browser -> .NET API -> Python FastAPI -> response.

Use for small match analysis and lightweight similarity queries.

## Asynchronous path
.NET -> job/queue -> Python worker -> analytical output -> PostgreSQL -> .NET API.

Use for post-match analysis, large historical queries, backtests, model training and nightly aggregates.

## Match-finished event
```text
MATCH_FINISHED
  -> reconcile result
  -> finalize odds
  -> create ANALYSIS_REQUEST
  -> Python calculates
  -> validate
  -> persist versioned result
  -> ANALYSIS_COMPLETED
```

## Idempotency
Every job carries jobId, analysisId, input version and attempt. Repeated delivery must not duplicate results.

## Queue strategy
Start with Hangfire for .NET-owned jobs. Introduce RabbitMQ only when independent Python consumers and durable cross-service queues justify the operational cost.

## Security
Python is private-network only. Use service authentication, correlation IDs and no provider credentials in requests.

## Observability
Propagate traceId, requestId, jobId, analysisId, matchId and provider across services.
