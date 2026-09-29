"""Implied probability model.

Decimal odds encode a bookmaker's implied probability of an outcome as the
reciprocal of the odds (``1 / odds``). The sum of the raw implied
probabilities across a complete set of mutually exclusive outcomes exceeds
1.0; the excess is the bookmaker's margin (the "overround" or "vigorish").

To recover fair probabilities we *normalize* the raw implied probabilities so
they sum to 1.0 (the basic multiplicative / proportional de-margining method).
"""

from __future__ import annotations

from collections.abc import Sequence


def implied_probabilities(odds: Sequence[float]) -> list[float]:
    """Convert decimal odds to raw implied probabilities.

    Each implied probability is ``1 / odds``. These are *not* normalized and
    will typically sum to more than 1.0 because of the bookmaker margin.

    Args:
        odds: Decimal odds, each strictly greater than 1.0.

    Returns:
        Raw implied probabilities, one per input odd.

    Raises:
        ValueError: If ``odds`` is empty or any odd is <= 1.0.
    """
    if not odds:
        raise ValueError("odds must be a non-empty sequence")
    probs: list[float] = []
    for o in odds:
        if o <= 1.0:
            raise ValueError(f"decimal odds must be > 1.0, got {o}")
        probs.append(1.0 / o)
    return probs


def normalize(probs: Sequence[float]) -> list[float]:
    """Normalize probabilities so they sum to 1.0.

    Uses the basic multiplicative (proportional) method: divide each raw
    probability by the total. This removes the bookmaker margin proportionally
    across all outcomes.

    Args:
        probs: Raw (unnormalized) probabilities.

    Returns:
        Probabilities scaled to sum to 1.0.

    Raises:
        ValueError: If ``probs`` is empty, contains a negative value, or sums
            to zero.
    """
    if not probs:
        raise ValueError("probs must be a non-empty sequence")
    total = 0.0
    for p in probs:
        if p < 0:
            raise ValueError(f"probabilities must be non-negative, got {p}")
        total += p
    if total <= 0:
        raise ValueError("sum of probabilities must be positive")
    return [p / total for p in probs]


def overround(odds: Sequence[float]) -> float:
    """Compute the bookmaker overround (margin multiplier) for a set of odds.

    The overround is the sum of raw implied probabilities. A fair book sums to
    exactly 1.0; anything above represents the bookmaker's margin. For example,
    an overround of 1.05 corresponds to a ~5% margin.

    Args:
        odds: Decimal odds, each strictly greater than 1.0.

    Returns:
        The sum of ``1 / odds`` across all outcomes.
    """
    return sum(implied_probabilities(odds))
