# Calcio-Analytic Analytics Service

Quantitative analytics service for Calcio-Analytic, a football odds/match
analytics platform. Built with FastAPI and the Python scientific stack
(Pandas, NumPy, SciPy, statsmodels, scikit-learn).

This is the **Phase 1 foundation**: a clean, runnable FastAPI skeleton with a
health endpoint. No real analytics logic yet.

## Requirements

- Python 3.12+

## Endpoints

| Method | Path      | Description                                          |
| ------ | --------- | ---------------------------------------------------- |
| GET    | `/health` | Liveness/readiness probe. Returns service status.    |
| GET    | `/`       | Basic service information (name, version, env).      |

Example responses:

```jsonc
// GET /health
{ "status": "healthy", "service": "analytics" }

// GET /
{ "name": "Calcio-Analytic Analytics Service", "version": "0.1.0", "env": "development" }
```

## Configuration

Settings are environment-driven via `pydantic-settings` (see `app/config.py`).
Values can be provided through environment variables or an optional `.env` file.

| Variable         | Default                                          | Description                              |
| ---------------- | ------------------------------------------------ | ---------------------------------------- |
| `ANALYTICS_PORT` | `8000`                                           | Port the service listens on.             |
| `DATABASE_URL`   | _(unset)_                                        | Optional database connection string.     |
| `APP_ENV`        | `development`                                    | Deployment environment.                  |
| `CORS_ORIGINS`   | `http://localhost:3000,http://localhost:5173`    | Comma-separated allowed CORS origins.    |

## Local development

Create and activate a virtual environment, then install dependencies.

### Windows (PowerShell)

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### macOS / Linux

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

### Run the service

```powershell
uvicorn app.main:app --reload --port 8000
```

The service is then available at http://localhost:8000, with interactive docs
at http://localhost:8000/docs.

## Tests

```powershell
pytest
```

## Docker

Build and run the container:

```powershell
docker build -t calcio-analytic-analytics .
docker run --rm -p 8000:8000 calcio-analytic-analytics
```

## Project layout

```
analytics/
├── app/
│   ├── __init__.py
│   ├── config.py       # pydantic-settings Settings
│   └── main.py         # FastAPI app + endpoints
├── tests/
│   ├── __init__.py
│   └── test_health.py  # health/root endpoint tests
├── Dockerfile
├── .dockerignore
├── pyproject.toml
├── requirements.txt
└── README.md
```
