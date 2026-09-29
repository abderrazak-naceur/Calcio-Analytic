# 29 — Python Analytics Service

## Purpose
Python is the quantitative computation service, not the domain database.

## Framework
FastAPI.

Default documentation routes are `/docs` for Swagger UI and `/openapi.json` for OpenAPI. citeturn0search1turn0search3

## Endpoints
- POST /v1/analyze/match
- POST /v1/analyze/pattern
- POST /v1/similarity/search
- POST /v1/backtests/run
- POST /v1/models/evaluate
- GET /health

## Libraries
FastAPI, Pydantic, Pandas, NumPy, SciPy, statsmodels, scikit-learn.
Optional: XGBoost, Polars, PyArrow after profiling.

## Match analysis
Input includes match context, odds snapshots, bookmaker prices, market lines, result, statistics and feature versions.
Output includes opening/closing analysis, movement, implied probabilities, overround, bookmaker dispersion, settlement, historical comparison and similarity.

## Historical pattern analysis
Every query must return sample size, filters, time range, distributions, methodology, completeness and analysis version.

## Backtesting
Strict point-in-time rules:
- no future result leakage
- no future standings/form
- no future odds
- no future statistics
- dataset, feature, model version and random seed recorded

## Persistence
Python writes only controlled analytical outputs. It must not directly mutate core domain tables.
