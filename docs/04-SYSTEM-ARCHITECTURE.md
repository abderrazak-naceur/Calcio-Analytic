# 04 — System Architecture

## Target stack
- Backend: .NET 10 / ASP.NET Core
- Database: PostgreSQL
- Cache: Redis
- Workers: .NET Worker + Hangfire o equivalente
- Frontend: React + TypeScript + Vite + Tailwind
- Containers: Docker

## Logical architecture
Provider APIs/feeds → Ingestion Workers → Raw Storage → Normalization → PostgreSQL → Analytics Services → REST API → React Web App

## Backend modules
Api, Application, Domain, Infrastructure, Ingestion, Analytics, Worker.

## Principles
Modular monolith first, boundaries chiare, job idempotenti, provider abstraction, analytics pesanti asincroni/pre-calcolati e cache per hot reads.

## Scalability
Partitioning delle time-series, aggregazioni pre-calcolate, object storage per raw cold data e scaling indipendente dei worker.
