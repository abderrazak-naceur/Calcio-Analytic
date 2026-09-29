"""Dixon-Coles adjustment on top of the independent-Poisson goals model.

The independent-Poisson model systematically misprices low-scoring results
(0-0, 1-0, 0-1, 1-1) because home and away goals are, in reality, weakly
dependent. Dixon and Coles introduced a correction factor ``tau`` controlled by
a single dependence parameter ``rho`` that reweights exactly those four
scorelines while leaving all other cells unchanged.

Reference:
    Dixon, M. J., & Coles, S. G. (1997). "Modelling Association Football Scores
    and Inefficiencies in the Football Betting Market." Journal of the Royal
    Statistical Society: Series C (Applied Statistics), 46(2), 265-280.

Only the ``tau`` correction is implemented here (not the time-decayed maximum
likelihood fitting of the original paper). The lambdas and ``rho`` are supplied
by the caller.
"""

from __future__ import annotations

import numpy as np

from app.models.poisson import score_matrix


def _tau(i: int, j: int, lambda_home: float, lambda_away: float, rho: float) -> float:
    """Dixon-Coles low-score correction factor for scoreline (i, j).

    Returns 1.0 for every scoreline other than the four low-score cells, so it
    leaves the bulk of the Poisson matrix untouched.
    """
    if i == 0 and j == 0:
        return 1.0 - lambda_home * lambda_away * rho
    if i == 0 and j == 1:
        return 1.0 + lambda_home * rho
    if i == 1 and j == 0:
        return 1.0 + lambda_away * rho
    if i == 1 and j == 1:
        return 1.0 - rho
    return 1.0


def dixon_coles_matrix(
    lambda_home: float,
    lambda_away: float,
    rho: float,
    max_goals: int = 10,
) -> np.ndarray:
    """Return the Dixon-Coles-adjusted joint scoreline probability matrix.

    Starts from the independent-Poisson :func:`~app.models.poisson.score_matrix`
    and multiplies the four low-score cells by their ``tau`` factors. The result
    is renormalized so it sums to 1.0 (the tau reweighting slightly changes the
    total mass). Negative cells that could arise from an extreme ``rho`` are
    clipped to 0.

    Args:
        lambda_home: Expected home goals (>= 0).
        lambda_away: Expected away goals (>= 0).
        rho: Low-score dependence parameter. Typically small and negative
            (e.g. around -0.1) in fitted models. ``rho == 0`` recovers the
            plain Poisson matrix.
        max_goals: Highest goal count modeled per team (inclusive).

    Returns:
        A normalized ``(max_goals + 1, max_goals + 1)`` numpy array.
    """
    matrix = score_matrix(lambda_home, lambda_away, max_goals).copy()
    for i in (0, 1):
        for j in (0, 1):
            matrix[i, j] *= _tau(i, j, lambda_home, lambda_away, rho)
    matrix = np.clip(matrix, 0.0, None)
    total = matrix.sum()
    if total <= 0:
        raise ValueError("adjusted matrix has non-positive total mass")
    return matrix / total


def dixon_coles_outcome_probabilities(
    lambda_home: float,
    lambda_away: float,
    rho: float,
    max_goals: int = 10,
) -> dict[str, float]:
    """Return {home, draw, away} probabilities from the adjusted matrix."""
    matrix = dixon_coles_matrix(lambda_home, lambda_away, rho, max_goals)
    home = float(np.tril(matrix, -1).sum())
    away = float(np.triu(matrix, 1).sum())
    draw = float(np.trace(matrix))
    return {"home": home, "draw": draw, "away": away}


def dixon_coles_over_under(
    lambda_home: float,
    lambda_away: float,
    rho: float,
    line: float = 2.5,
    max_goals: int = 10,
) -> dict[str, float]:
    """Return {over, under} total-goals probabilities from the adjusted matrix."""
    matrix = dixon_coles_matrix(lambda_home, lambda_away, rho, max_goals)
    n = matrix.shape[0]
    over = 0.0
    for i in range(n):
        for j in range(n):
            if (i + j) > line:
                over += matrix[i, j]
    over_p = float(over)
    return {"over": over_p, "under": 1.0 - over_p}
