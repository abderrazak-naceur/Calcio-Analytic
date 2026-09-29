"""API tests for the models and backtests routers using TestClient."""

from __future__ import annotations

import pytest
from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def _sum_1x2(one_x_two: dict) -> float:
    return one_x_two["home"] + one_x_two["draw"] + one_x_two["away"]


# --------------------------------------------------------------------------- #
# /api/v1/models/*
# --------------------------------------------------------------------------- #
def test_implied_endpoint():
    resp = client.post("/api/v1/models/implied", json={"odds": [2.10, 3.40, 3.60]})
    assert resp.status_code == 200
    body = resp.json()
    assert body["overround"] == pytest.approx(1.048, abs=1e-3)
    assert sum(body["normalized"]) == pytest.approx(1.0)
    assert len(body["implied"]) == 3


def test_poisson_endpoint():
    resp = client.post(
        "/api/v1/models/poisson",
        json={"lambda_home": 1.6, "lambda_away": 1.1, "ou_line": 2.5},
    )
    assert resp.status_code == 200
    body = resp.json()
    assert _sum_1x2(body["one_x_two"]) == pytest.approx(1.0)
    ou = body["over_under"]
    assert ou["over"] + ou["under"] == pytest.approx(1.0)


def test_dixon_coles_endpoint():
    resp = client.post(
        "/api/v1/models/dixon-coles",
        json={"lambda_home": 1.6, "lambda_away": 1.1, "rho": -0.1, "ou_line": 2.5},
    )
    assert resp.status_code == 200
    body = resp.json()
    assert _sum_1x2(body["one_x_two"]) == pytest.approx(1.0)
    ou = body["over_under"]
    assert ou["over"] + ou["under"] == pytest.approx(1.0)


def test_elo_predict_endpoint():
    resp = client.post(
        "/api/v1/models/elo/predict",
        json={"home_rating": 1600.0, "away_rating": 1500.0},
    )
    assert resp.status_code == 200
    body = resp.json()
    assert _sum_1x2(body["one_x_two"]) == pytest.approx(1.0)
    assert all(0.0 <= v <= 1.0 for v in body["one_x_two"].values())


# --------------------------------------------------------------------------- #
# /api/v1/backtests/
# --------------------------------------------------------------------------- #
def test_backtest_poisson_endpoint():
    payload = {
        "model": "poisson",
        "params": {},
        "matches": [
            {"lambda_home": 1.8, "lambda_away": 0.9, "outcome": "home"},
            {"lambda_home": 1.0, "lambda_away": 1.0, "outcome": "draw"},
            {"lambda_home": 0.8, "lambda_away": 1.7, "outcome": "away"},
        ],
    }
    resp = client.post("/api/v1/backtests/", json=payload)
    assert resp.status_code == 200
    body = resp.json()
    assert body["n_records"] == 3
    assert body["log_loss"] >= 0.0
    assert 0.0 <= body["brier_score"] <= 2.0
    assert 0.0 <= body["accuracy"] <= 1.0


def test_backtest_elo_endpoint():
    payload = {
        "model": "elo",
        "params": {},
        "matches": [
            {"home_rating": 1800.0, "away_rating": 1400.0, "outcome": "home"},
            {"home_rating": 1500.0, "away_rating": 1500.0, "outcome": "draw"},
        ],
    }
    resp = client.post("/api/v1/backtests/", json=payload)
    assert resp.status_code == 200
    body = resp.json()
    assert body["n_records"] == 2
    assert 0.0 <= body["accuracy"] <= 1.0


def test_backtest_implied_endpoint():
    payload = {
        "model": "implied",
        "params": {},
        "matches": [
            {"odds": [2.10, 3.40, 3.60], "outcome": "home"},
            {"odds": [3.00, 3.30, 2.30], "outcome": "away"},
        ],
    }
    resp = client.post("/api/v1/backtests/", json=payload)
    assert resp.status_code == 200
    body = resp.json()
    assert body["n_records"] == 2


def test_backtest_unknown_model_returns_422():
    payload = {"model": "nope", "params": {}, "matches": [{"outcome": "home"}]}
    resp = client.post("/api/v1/backtests/", json=payload)
    assert resp.status_code == 422
