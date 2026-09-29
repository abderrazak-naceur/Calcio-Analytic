"""Independent-Poisson goals model.

Each team's goals scored in a match are modeled as an independent Poisson
random variable with a team-specific mean (``lambda_home`` / ``lambda_away``).
The joint scoreline distribution is the outer product of the two marginal
Poisson pmfs, from which we derive 1X2 (home/draw/away) and Over/Under
probabilities.

The expected goals (lambdas) can be supplied directly, or derived from
attack/defense strength factors and a league baseline via
:func:`lambdas_from_strengths`.
"""

from __future__ import annotations

import numpy as np
from scipy.stats import poisson


def lambdas_from_strengths(
    home_attack: float,
    home_defense: float,
    away_attack: float,
    away_defense: float,
    league_avg_goals: float = 1.35,
    home_advantage: float = 1.1,
) -> tuple[float, float]:
    """Derive (lambda_home, lambda_away) from attack/defense strengths.

    Strengths are multiplicative factors relative to a league-average team
    (1.0 == average). Expected home goals scale with the home attack, the away
    defense, the league baseline, and a home-advantage multiplier.

    Args:
        home_attack: Home team's attacking strength factor.
        home_defense: Home team's defensive strength factor.
        away_attack: Away team's attacking strength factor.
        away_defense: Away team's defensive strength factor.
        league_avg_goals: Baseline expected goals per team per match.
        home_advantage: Multiplier applied to the home team's expected goals.

    Returns:
        A ``(lambda_home, lambda_away)`` tuple of expected goals.
    """
    lambda_home = league_avg_goals * home_attack * away_defense * home_advantage
    lambda_away = league_avg_goals * away_attack * home_defense
    return lambda_home, lambda_away


def score_matrix(lambda_home: float, lambda_away: float, max_goals: int = 10) -> np.ndarray:
    """Return the joint scoreline probability matrix.

    Entry ``[i, j]`` is the probability of the home team scoring ``i`` goals
    and the away team scoring ``j`` goals, for ``i, j`` in ``0..max_goals``.
    Because goals are truncated at ``max_goals`` the matrix sums to slightly
    less than 1.0; the tail mass is negligible for realistic lambdas.

    Args:
        lambda_home: Expected home goals (>= 0).
        lambda_away: Expected away goals (>= 0).
        max_goals: Highest goal count modeled per team (inclusive).

    Returns:
        A ``(max_goals + 1, max_goals + 1)`` numpy array.

    Raises:
        ValueError: If a lambda is negative or ``max_goals`` < 1.
    """
    if lambda_home < 0 or lambda_away < 0:
        raise ValueError("lambdas must be non-negative")
    if max_goals < 1:
        raise ValueError("max_goals must be >= 1")
    goals = np.arange(0, max_goals + 1)
    home_pmf = poisson.pmf(goals, lambda_home)
    away_pmf = poisson.pmf(goals, lambda_away)
    return np.outer(home_pmf, away_pmf)


def outcome_probabilities(
    lambda_home: float,
    lambda_away: float,
    max_goals: int = 10,
) -> dict[str, float]:
    """Return {home, draw, away} probabilities from the scoreline matrix.

    Probabilities are renormalized over the truncated matrix so they sum to
    exactly 1.0.
    """
    matrix = score_matrix(lambda_home, lambda_away, max_goals)
    total = matrix.sum()
    home = float(np.tril(matrix, -1).sum())  # home goals > away goals
    away = float(np.triu(matrix, 1).sum())  # away goals > home goals
    draw = float(np.trace(matrix))  # equal goals
    return {
        "home": home / total,
        "draw": draw / total,
        "away": away / total,
    }


def over_under(
    lambda_home: float,
    lambda_away: float,
    line: float = 2.5,
    max_goals: int = 10,
) -> dict[str, float]:
    """Return {over, under} probabilities for a total-goals line.

    A total exactly equal to ``line`` counts as "under". For a half-goal line
    such as 2.5 there are no ties, which is the common case.

    Args:
        lambda_home: Expected home goals.
        lambda_away: Expected away goals.
        line: Total-goals threshold (e.g. 2.5).
        max_goals: Highest goal count modeled per team.

    Returns:
        A dict with ``over`` and ``under`` probabilities summing to 1.0.
    """
    matrix = score_matrix(lambda_home, lambda_away, max_goals)
    total = matrix.sum()
    n = matrix.shape[0]
    over = 0.0
    for i in range(n):
        for j in range(n):
            if (i + j) > line:
                over += matrix[i, j]
    over_p = float(over) / total
    return {"over": over_p, "under": 1.0 - over_p}
