"""Quantitative football models.

This package hosts the baseline statistical models for the Calcio-Analytic
analytics service:

- ``implied``: convert decimal odds to (normalized) implied probabilities.
- ``elo``: ELO rating model with a simple 1X2 predictor.
- ``poisson``: independent-Poisson goals model.
- ``dixon_coles``: Dixon-Coles low-score correlation adjustment.
"""

from __future__ import annotations

from app.models.dixon_coles import (
    dixon_coles_matrix,
    dixon_coles_outcome_probabilities,
)
from app.models.elo import EloModel
from app.models.implied import implied_probabilities, normalize, overround
from app.models.poisson import outcome_probabilities, over_under, score_matrix

__all__ = [
    "implied_probabilities",
    "normalize",
    "overround",
    "EloModel",
    "score_matrix",
    "outcome_probabilities",
    "over_under",
    "dixon_coles_matrix",
    "dixon_coles_outcome_probabilities",
]
