"""FastAPI application entrypoint for the Calcio-Analytic analytics service."""

from __future__ import annotations

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api.backtest_router import router as backtest_router
from app.api.models_router import router as models_router
from app.config import settings

APP_NAME = "Calcio-Analytic Analytics Service"
APP_VERSION = "0.1.0"

app = FastAPI(title=APP_NAME, version=APP_VERSION)

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(models_router)
app.include_router(backtest_router)


@app.get("/health")
def health() -> dict[str, str]:
    """Liveness/readiness probe."""
    return {"status": "healthy", "service": "analytics"}


@app.get("/")
def root() -> dict[str, str]:
    """Basic service information."""
    return {
        "name": APP_NAME,
        "version": APP_VERSION,
        "env": settings.app_env,
    }
