"""Evaluation metrics for 1X2 probability forecasts and numeric predictions.

The 1X2 metrics operate on:
- ``probs``: an ``(n, 3)`` array-like of forecast probabilities, columns
  ordered ``[home, draw, away]`` and each row summing (approximately) to 1.0.
- ``outcomes``: length-``n`` integer labels, ``0=home``, ``1=draw``, ``2=away``.

The numeric metrics (``rmse``, ``mae``) operate on two equal-length numeric
arrays.
"""

from __future__ import annotations

from collections.abc import Sequence

import numpy as np

# Canonical column order / label encoding for 1X2 forecasts.
OUTCOME_INDEX = {"home": 0, "draw": 1, "away": 2}

_EPS = 1e-15


def _as_prob_matrix(probs: Sequence[Sequence[float]]) -> np.ndarray:
    arr = np.asarray(probs, dtype=float)
    if arr.ndim != 2 or arr.shape[1] != 3:
        raise ValueError("probs must have shape (n, 3) for [home, draw, away]")
    return arr


def _as_outcomes(outcomes: Sequence[int]) -> np.ndarray:
    arr = np.asarray(outcomes, dtype=int)
    if arr.ndim != 1:
        raise ValueError("outcomes must be a 1-D sequence of class labels")
    if arr.size and (arr.min() < 0 or arr.max() > 2):
        raise ValueError("outcome labels must be in {0=home, 1=draw, 2=away}")
    return arr


def log_loss(probs: Sequence[Sequence[float]], outcomes: Sequence[int]) -> float:
    """Multiclass log loss (cross-entropy) for 1X2 forecasts.

    Lower is better; a perfect, confident forecast approaches 0. Probabilities
    are clipped to ``[eps, 1]`` to avoid ``log(0)``.
    """
    p = _as_prob_matrix(probs)
    y = _as_outcomes(outcomes)
    if p.shape[0] != y.shape[0]:
        raise ValueError("probs and outcomes must have the same length")
    if y.size == 0:
        raise ValueError("cannot compute log_loss on empty input")
    picked = p[np.arange(y.shape[0]), y]
    picked = np.clip(picked, _EPS, 1.0)
    return float(-np.mean(np.log(picked)))


def brier_score(probs: Sequence[Sequence[float]], outcomes: Sequence[int]) -> float:
    """Multiclass Brier score for 1X2 forecasts.

    Mean squared error between the forecast probability vector and the one-hot
    actual outcome, averaged over samples. Range ``[0, 2]``; lower is better.
    """
    p = _as_prob_matrix(probs)
    y = _as_outcomes(outcomes)
    if p.shape[0] != y.shape[0]:
        raise ValueError("probs and outcomes must have the same length")
    if y.size == 0:
        raise ValueError("cannot compute brier_score on empty input")
    one_hot = np.zeros_like(p)
    one_hot[np.arange(y.shape[0]), y] = 1.0
    return float(np.mean(np.sum((p - one_hot) ** 2, axis=1)))


def accuracy(probs: Sequence[Sequence[float]], outcomes: Sequence[int]) -> float:
    """Fraction of samples whose argmax forecast matches the actual outcome."""
    p = _as_prob_matrix(probs)
    y = _as_outcomes(outcomes)
    if p.shape[0] != y.shape[0]:
        raise ValueError("probs and outcomes must have the same length")
    if y.size == 0:
        raise ValueError("cannot compute accuracy on empty input")
    predicted = np.argmax(p, axis=1)
    return float(np.mean(predicted == y))


def rmse(predicted: Sequence[float], actual: Sequence[float]) -> float:
    """Root mean squared error between two numeric arrays."""
    yp = np.asarray(predicted, dtype=float)
    ya = np.asarray(actual, dtype=float)
    if yp.shape != ya.shape:
        raise ValueError("predicted and actual must have the same shape")
    if yp.size == 0:
        raise ValueError("cannot compute rmse on empty input")
    return float(np.sqrt(np.mean((yp - ya) ** 2)))


def mae(predicted: Sequence[float], actual: Sequence[float]) -> float:
    """Mean absolute error between two numeric arrays."""
    yp = np.asarray(predicted, dtype=float)
    ya = np.asarray(actual, dtype=float)
    if yp.shape != ya.shape:
        raise ValueError("predicted and actual must have the same shape")
    if yp.size == 0:
        raise ValueError("cannot compute mae on empty input")
    return float(np.mean(np.abs(yp - ya)))
