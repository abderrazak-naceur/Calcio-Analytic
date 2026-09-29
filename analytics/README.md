# Calcio-Analytic Analytics Service

Quantitative analytics service for Calcio-Analytic, a football odds/match
analytics platform. Built with FastAPI and the Python scientific stack
(Pandas, NumPy, SciPy, statsmodels, scikit-learn).

It exposes the baseline quantitative models (implied probabilities, Poisson,
Dixon-Coles, ELO) and a leakage-safe backtesting engine over a small HTTP API.

## Requirements

- Python 3.12+ recommended. The pinned `requirements.txt` targets 3.12+; the
  code itself runs on Python 3.9+ (it uses `from __future__ import annotations`
  for modern type-hint syntax) with compatible NumPy/SciPy builds.

## Endpoints

### Service

| Method | Path      | Description                                          |
| ------ | --------- | ---------------------------------------------------- |
| GET    | `/health` | Liveness/readiness probe. Returns service status.    |
| GET    | `/`       | Basic service information (name, version, env).      |

### Models — prefix `/api/v1/models`

| Method | Path             | Description                                                       |
| ------ | ---------------- | ----------------------------------------------------------------- |
| POST   | `/implied`       | Decimal odds -> implied + normalized probabilities and overround. |
| POST   | `/poisson`       | Independent-Poisson 1X2 + Over/Under probabilities.               |
| POST   | `/dixon-coles`   | Dixon-Coles-adjusted 1X2 + Over/Under probabilities.              |
| POST   | `/elo/predict`   | ELO 1X2 probabilities from home/away ratings.                     |

### Backtests — prefix `/api/v1/backtests`

| Method | Path | Description                                                            |
| ------ | ---- | --------------------------------------------------------------------- |
| POST   | `/`  | Run a leakage-safe backtest and return `log_loss`, `brier_score`, `accuracy`. |

Example responses:

```jsonc
// GET /health
{ "status": "healthy", "service": "analytics" }

// GET /
{ "name": "Calcio-Analytic Analytics Service", "version": "0.1.0", "env": "development" }

// POST /api/v1/models/implied  { "odds": [2.10, 3.40, 3.60] }
{
  "implied": [0.4762, 0.2941, 0.2778],
  "normalized": [0.4544, 0.2806, 0.2650],
  "overround": 1.0481
}

// POST /api/v1/models/poisson  { "lambda_home": 1.6, "lambda_away": 1.1, "ou_line": 2.5 }
{
  "one_x_two": { "home": 0.48, "draw": 0.25, "away": 0.27 },
  "over_under": { "line": 2.5, "over": 0.58, "under": 0.42 }
}

// POST /api/v1/models/dixon-coles  { "lambda_home": 1.6, "lambda_away": 1.1, "rho": -0.1, "ou_line": 2.5 }
{
  "one_x_two": { "home": 0.47, "draw": 0.26, "away": 0.27 },
  "over_under": { "line": 2.5, "over": 0.57, "under": 0.43 }
}

// POST /api/v1/models/elo/predict  { "home_rating": 1600, "away_rating": 1500 }
{ "one_x_two": { "home": 0.49, "draw": 0.27, "away": 0.24 } }

// POST /api/v1/backtests/
// {
//   "model": "poisson",
//   "params": {},
//   "matches": [
//     { "lambda_home": 1.8, "lambda_away": 0.9, "outcome": "home" },
//     { "lambda_home": 1.0, "lambda_away": 1.0, "outcome": "draw" }
//   ]
// }
{ "log_loss": 0.71, "brier_score": 0.55, "accuracy": 1.0, "n_records": 2 }
```

The backtest `model` field accepts `implied`, `poisson`, `dixon_coles`
(or `dixon-coles`), and `elo`. Each match provides the feature fields that
model needs plus an `outcome` in `{"home", "draw", "away"}`; an optional
`timestamp` is accepted. The engine enforces an anti-leakage cutoff and never
passes the `outcome` to the model. Numeric probability values above are
illustrative.

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
│   ├── config.py              # pydantic-settings Settings
│   ├── main.py                # FastAPI app + endpoints + routers
│   ├── api/
│   │   ├── models_router.py   # /api/v1/models
│   │   └── backtest_router.py # /api/v1/backtests
│   ├── models/                # implied, poisson, dixon_coles, elo
│   └── backtesting/           # metrics + leakage-safe engine
├── tests/
│   ├── __init__.py
│   ├── test_health.py         # health/root endpoint tests
│   ├── test_models.py         # model + metrics unit tests
│   └── test_models_api.py     # models + backtests API tests
├── Dockerfile
├── .dockerignore
├── pyproject.toml
├── requirements.txt
└── README.md
```
