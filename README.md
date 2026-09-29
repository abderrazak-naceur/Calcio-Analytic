# Calcio-Analytic

Historical football odds and match analytics platform.

Calcio-Analytic ingests football data (catalog, matches, odds, statistics) from
pluggable providers, preserves complete point-in-time history, reconciles results,
and produces reproducible, versioned analytical reports exposed through a REST API
and a React web application.

## Architecture

```
Provider APIs/feeds
  -> Ingestion Workers (.NET)
  -> Raw Storage + Normalization
  -> PostgreSQL (system of record)
  -> Analytics (.NET + Python/FastAPI)
  -> REST API (.NET, OpenAPI 3.1)
  -> React + TypeScript + Vite + Tailwind Web App
```

Backend follows a modular monolith with clean boundaries:

| Project                        | Responsibility                                    |
| ------------------------------ | ------------------------------------------------- |
| `CalcioAnalytic.Domain`        | Entities, value objects, domain rules             |
| `CalcioAnalytic.Application`   | Use cases, orchestration, interfaces              |
| `CalcioAnalytic.Contracts`     | Provider-facing DTOs and canonical contracts      |
| `CalcioAnalytic.Infrastructure`| EF Core, PostgreSQL, Redis, persistence           |
| `CalcioAnalytic.Ingestion`     | Provider adapters and ingestion pipeline          |
| `CalcioAnalytic.Analytics`     | Analysis engines                                  |
| `CalcioAnalytic.Api`           | ASP.NET Core REST API + OpenAPI                   |
| `CalcioAnalytic.Workers`       | Background jobs / scheduled ingestion             |

Dependency rule: `Domain` depends on nothing. Provider DTOs never leak into `Domain`.

## Tech stack

- Backend: .NET 10 / ASP.NET Core, EF Core, OpenAPI 3.1
- Database: PostgreSQL
- Cache: Redis
- Quantitative analytics: Python + FastAPI
- Frontend: React + TypeScript + Vite + Tailwind CSS
- Containers: Docker Compose
- CI: GitHub Actions

## Local development

Prerequisites: .NET 10 SDK, Node.js 20+, Python 3.11+, Docker.

1. Copy environment file:

   ```bash
   cp .env.example .env
   ```

2. Start infrastructure (PostgreSQL + Redis):

   ```bash
   docker compose up -d postgres redis
   ```

3. Run the .NET API:

   ```bash
   dotnet run --project backend/src/CalcioAnalytic.Api
   ```

   Health: `GET http://localhost:8080/health`
   OpenAPI: `http://localhost:8080/swagger`

4. Run the Python analytics service:

   ```bash
   cd analytics
   python -m venv .venv && . .venv/Scripts/activate   # Windows: .venv\Scripts\Activate.ps1
   pip install -r requirements.txt
   uvicorn app.main:app --reload --port 8000
   ```

   Health: `GET http://localhost:8000/health`

5. Run the frontend:

   ```bash
   cd frontend
   npm install
   npm run dev
   ```

Or start everything with Docker Compose:

```bash
docker compose up --build
```

## Repository layout

```
backend/    .NET 10 solution (API, workers, domain, analytics)
frontend/   React + Vite + Tailwind web application
analytics/  Python + FastAPI quantitative service
docs/        Product, architecture and implementation specifications
```

## Development principle

Build the smallest complete vertical slice first, then expand coverage.
Historical data is append-oriented, raw payloads are immutable, and analytical
outputs are versioned. See `docs/33-MASTER-IMPLEMENTATION-ROADMAP.md`.
