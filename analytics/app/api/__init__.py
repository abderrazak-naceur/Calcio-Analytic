"""FastAPI routers for the analytics service."""

from __future__ import annotations

from app.api.backtest_router import router as backtest_router
from app.api.models_router import router as models_router

__all__ = ["models_router", "backtest_router"]
