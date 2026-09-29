"""Unit tests for the baseline quantitative models and evaluation metrics.

These tests exercise the actual function signatures in ``app/models`` and
``app/backtesting/metrics.py``.
"""

from __future__ import annotations

import pytest

from app.backtesting.metrics import brier_score, log_loss
from app.models.dixon_coles import dixon_coles_outcome_probabilities
from app.models.elo import EloModel
from app.models.implied import implied_probabilities, normalize, overround
from app.models.poisson import outcome_probabilities


# --------------------------------------------------------------------------- #
# Implied
# --------------------------------------------------------------------------- #
def test_implied_overround():
    odds = [2.10, 3.40, 3.60]
    assert overround(odds) == pytest.approx(1.048, abs=1e-3)


def test_implied_normalized_sums_to_one():
    odds = [2.10, 3.40, 3.60]
    normalized = normalize(implied_probabilities(odds))
    assert sum(normalized) == pytest.approx(1.0)
    assert all(0.0 <= p <= 1.0 for p in normalized)


# --------------------------------------------------------------------------- #
# Poisson
# --------------------------------------------------------------------------- #
def test_poisson_outcome_probabilities_sum_to_one():
    probs = outcome_probabilities(1.6, 1.1)
    assert set(probs) == {"home", "draw", "away"}
    assert sum(probs.values()) == pytest.approx(1.0)
    assert all(0.0 <= p <= 1.0 for p in probs.values())


# --------------------------------------------------------------------------- #
# Dixon-Coles
# --------------------------------------------------------------------------- #
def test_dixon_coles_outcome_probabilities_valid_and_sum_to_one():
    probs = dixon_coles_outcome_probabilities(1.6, 1.1, rho=-0.1)
    assert set(probs) == {"home", "draw", "away"}
    assert sum(probs.values()) == pytest.approx(1.0)
    assert all(0.0 <= p <= 1.0 for p in probs.values())


# --------------------------------------------------------------------------- #
# ELO
# --------------------------------------------------------------------------- #
def test_elo_predict_probabilities_in_range_and_sum_to_one():
    model = EloModel()
    probs = model.predict_1x2(1600.0, 1500.0)
    assert set(probs) == {"home", "draw", "away"}
    assert sum(probs.values()) == pytest.approx(1.0)
    assert all(0.0 <= p <= 1.0 for p in probs.values())


def test_elo_stronger_home_team_more_likely_to_win():
    model = EloModel()
    probs = model.predict_1x2(1800.0, 1400.0)
    assert probs["home"] > probs["away"]


# --------------------------------------------------------------------------- #
# Metrics
# --------------------------------------------------------------------------- #
def test_metrics_perfect_forecast():
    # Confident, correct forecasts -> near-zero loss/brier.
    probs = [[1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]]
    outcomes = [0, 1, 2]
    assert log_loss(probs, outcomes) == pytest.approx(0.0, abs=1e-9)
    assert brier_score(probs, outcomes) == pytest.approx(0.0, abs=1e-9)


def test_metrics_uniform_forecast():
    # Uniform 1/3 forecasts -> log_loss == ln(3), brier == 2/3.
    import math

    probs = [[1 / 3, 1 / 3, 1 / 3]] * 3
    outcomes = [0, 1, 2]
    assert log_loss(probs, outcomes) == pytest.approx(math.log(3.0))
    assert brier_score(probs, outcomes) == pytest.approx(2.0 / 3.0)


def test_metrics_confident_wrong_worse_than_uniform():
    import math

    wrong = [[0.01, 0.495, 0.495]]  # true outcome is home (index 0)
    uniform = [[1 / 3, 1 / 3, 1 / 3]]
    outcomes = [0]
    assert log_loss(wrong, outcomes) > log_loss(uniform, outcomes)
    assert log_loss(uniform, outcomes) == pytest.approx(math.log(3.0))
