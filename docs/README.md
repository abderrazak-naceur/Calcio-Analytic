# Calcio-Analytic Documentation

Questa cartella contiene la documentazione completa del prodotto e del progetto.

## Document map

- `01-PRODUCT-VISION.md` — visione, obiettivi, utenti e value proposition.
- `02-BUSINESS-PLAN.md` — business model, pricing, costi, revenue e validazione.
- `03-FUNCTIONAL-SPECIFICATION.md` — requisiti funzionali completi.
- `04-SYSTEM-ARCHITECTURE.md` — architettura software e infrastruttura.
- `05-DATA-ARCHITECTURE.md` — PostgreSQL, data warehouse e time series.
- `06-DATA-PROVIDERS.md` — provider abstraction, ingestion e provenance.
- `07-DATA-MODEL.md` — entità e relazioni principali.
- `08-ODDS-ANALYTICS.md` — quote, probabilità implicite e movimenti.
- `09-TEAM-MATCH-ANALYTICS.md` — squadre, partite, forma e statistiche.
- `10-MODELS-BACKTESTING.md` — modelli, backtesting e anti-look-ahead.
- `11-WEB-UX-UI-SPECIFICATION.md` — dashboard, pagine, componenti e UX.
- `12-API-SPECIFICATION.md` — REST API e contratti principali.
- `13-AUTH-SECURITY.md` — autenticazione, autorizzazione e sicurezza.
- `14-DATA-QUALITY.md` — qualità, reconciliation e monitoring.
- `15-DEVOPS-DEPLOYMENT.md` — Docker, CI/CD, observability e deployment.
- `16-ROADMAP.md` — roadmap MVP → SaaS → advanced analytics.
- `17-REQUIREMENTS-TRACEABILITY.md` — matrice requisiti → implementazione → test.
- `18-LEGAL-DATA-GOVERNANCE.md` — data licensing, provenance e governance.
- `19-AI-ANALYTICS.md` — AI assistant e natural-language analytics.
- `20-ADMIN-SPECIFICATION.md` — area amministrativa e operations.
- `21-API-CUSTOMER.md` — API customer e query analytics.
- `22-TESTING-STRATEGY.md` — unit, integration, E2E e data tests.
- `23-OBSERVABILITY.md` — logging, metrics, tracing e alerting.
- `24-IMPLEMENTATION-PLAN.md` — ordine esatto di sviluppo.
- `25-DETAILED-MATCH-DATA-COVERAGE.md` — copertura dettagliata di dati, mercati e quote per partita.
- `26-ANALYTICS-ENGINE.md` — motore di elaborazione post-match, analisi quote, pattern storici e backtesting.
- `27-TECHNOLOGY-STACK.md` — stack tecnologico e responsabilità dei servizi.
- `28-DOTNET-API-SWAGGER.md` — API .NET, OpenAPI e Swagger.
- `29-PYTHON-ANALYTICS-SERVICE.md` — servizio Python/FastAPI per analisi quantitative.
- `30-SERVICE-COMMUNICATION.md` — comunicazione sincrona/asincrona tra .NET e Python.
- `31-FRONTEND-VITE-ARCHITECTURE.md` — architettura React/Vite.
- `32-DATA-ANALYSIS-FLOW.md` — ciclo completo dalla raccolta dati al report storico.

## Source of truth

`01-PRODUCT-VISION.md` definisce il prodotto.
`03-FUNCTIONAL-SPECIFICATION.md` definisce cosa deve fare.
`04-SYSTEM-ARCHITECTURE.md` definisce come costruirlo.
`25-DETAILED-MATCH-DATA-COVERAGE.md` definisce quali dati dobbiamo conservare.
`26-ANALYTICS-ENGINE.md` definisce come trasformare quei dati in analisi riproducibili.

## Repository execution

`AGENT_TASKS.md` contiene il backlog implementativo P0/P1/P2. Una task non deve essere marcata DONE senza implementazione, test, documentazione e verifica.

## Current repository status

La documentazione costituisce la base per l'implementazione del backend .NET 10, ingestion engine, Analytics Engine, PostgreSQL data layer, worker services e web application React/TypeScript.
