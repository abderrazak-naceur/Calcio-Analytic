"""ELO rating model for football teams.

The ELO system models the relative strength of two teams as a rating
difference. The expected score (win probability on a 0..1 scale) of the home
team is a logistic function of the rating gap, with an additive home-advantage
bonus applied to the home rating.

Draw handling
-------------
Standard ELO produces only a single "expected score" in [0, 1] and has no
native notion of a draw. For a 1X2 (home/draw/away) forecast we use a simple,
explicit draw model: the draw probability is a fixed base value that peaks when
the two (advantage-adjusted) expected scores are close and shrinks toward 0 as
one side dominates. The home/away win probabilities are then the remaining mass
split in proportion to each side's expected score.

This is a deliberately simple, documented assumption -- not a calibrated draw
model. For calibrated scoreline forecasts use the Poisson / Dixon-Coles models.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass
class EloModel:
    """Configurable ELO rating model.

    Attributes:
        k_factor: Update step size. Larger values react faster to results.
        home_advantage: Rating points added to the home team for expectations.
        base_draw: Base draw probability used by the simple 1X2 draw model,
            realized when the two sides are evenly matched.
    """

    k_factor: float = 20.0
    home_advantage: float = 65.0
    base_draw: float = 0.28

    def expected_score(self, home_rating: float, away_rating: float) -> float:
        """Return the home team's expected score in [0, 1].

        The expected score is the standard logistic ELO expectation using a
        400-point scale, with ``home_advantage`` added to the home rating.
        """
        diff = (home_rating + self.home_advantage) - away_rating
        return 1.0 / (1.0 + 10.0 ** (-diff / 400.0))

    def update(
        self,
        home_rating: float,
        away_rating: float,
        home_goals: int,
        away_goals: int,
    ) -> tuple[float, float]:
        """Return updated (home_rating, away_rating) after a result.

        The actual score is 1.0 for a home win, 0.5 for a draw, 0.0 for an
        away win. Ratings move by ``k_factor * (actual - expected)`` and the
        away team moves by the exact opposite amount (zero-sum).
        """
        expected_home = self.expected_score(home_rating, away_rating)
        if home_goals > away_goals:
            actual_home = 1.0
        elif home_goals < away_goals:
            actual_home = 0.0
        else:
            actual_home = 0.5
        delta = self.k_factor * (actual_home - expected_home)
        return home_rating + delta, away_rating - delta

    def predict_1x2(self, home_rating: float, away_rating: float) -> dict[str, float]:
        """Return {home, draw, away} probabilities that sum to 1.0.

        Uses the simple draw model documented in the module docstring: the draw
        probability shrinks as the expected score moves away from 0.5, and the
        remaining probability mass is split between home and away in proportion
        to the home expected score.
        """
        exp_home = self.expected_score(home_rating, away_rating)
        # Draw is most likely for evenly matched sides (exp_home == 0.5) and
        # falls off symmetrically toward decisive matchups.
        draw = self.base_draw * (1.0 - 2.0 * abs(exp_home - 0.5))
        draw = max(0.0, min(draw, 1.0))
        remaining = 1.0 - draw
        home = remaining * exp_home
        away = remaining * (1.0 - exp_home)
        return {"home": home, "draw": draw, "away": away}
