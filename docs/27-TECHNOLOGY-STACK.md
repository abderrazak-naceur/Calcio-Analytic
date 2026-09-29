# 27 — Technology Stack

## Decision
- Frontend: React + TypeScript + Vite + Tailwind CSS.
- Main backend: ASP.NET Core .NET 10.
- API contract: OpenAPI 3.1.
- Interactive documentation: Swagger UI.
- Database: PostgreSQL.
- Cache/coordination: Redis.
- Background jobs: .NET Worker Services + Hangfire initially.
- Quantitative analytics: Python + FastAPI.
- Statistics: Pandas, NumPy, SciPy, statsmodels.
- ML: scikit-learn, with XGBoost only where justified.
- Containers: Docker Compose.
- Observability: OpenTelemetry + structured logging.
- CI/CD: GitHub Actions.

ASP.NET Core .NET 10 provides first-party OpenAPI generation and OpenAPI 3.1 support; Swagger UI remains an optional interactive layer. citeturn0search4turn0search7
Vite officially supports React + TypeScript templates. citeturn0search6
FastAPI provides OpenAPI and interactive Swagger UI. citeturn0search1turn0search3

## Ownership
.NET owns authentication, domain, ingestion, persistence, jobs, public API and application workflows.
Python owns statistical computation, feature engineering, similarity, model training/evaluation and backtesting.
Python is never the domain system of record.

## Constraints
- Provider DTOs never leak into Domain.
- Historical data is append-oriented.
- Raw payloads are immutable.
- Analytical outputs are versioned.
- Provider secrets never reach the browser.
- Large history tables must be designed for future partitioning.
