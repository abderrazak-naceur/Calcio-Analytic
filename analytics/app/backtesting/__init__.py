"""Backtesting utilities: evaluation metrics and a leakage-safe runner."""

from __future__ import annotations

from app.backtesting.engine import Backtest, BacktestResult, run
from app.backtesting.metrics import (
    accuracy,
    brier_score,
    log_loss,
    mae,
    rmse,
)

__all__ = [
    "Backtest",
    "BacktestResult",
    "run",
    "accuracy",
    "brier_score",
    "log_loss",
    "mae",
    "rmse",
]
