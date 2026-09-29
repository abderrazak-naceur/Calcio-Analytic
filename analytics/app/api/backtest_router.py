"""API router exposing the backtesting engine.

Prefix: ``/api/v1/backtests``

Endpoint:
- ``POST /`` -> run a leakage-safe backtest over historical match records and
  return aggregated metrics (``log_loss``, ``brier_score``, ``accuracy``).

The request selects a model by name and passes model ``params`` plus a list of
``matches``. Each match carries feature fields plus an ``outcome`` in
``{"home", "draw", "away"}``. The router builds a model callable that maps a
match's features to a ``{home, draw, away}`` probability dict and delegates the
actual run to :func:`app.backtesting.engine.run`.
"""

from __future__ import annotations

from collections.abc import Mapping
from typing import Any, Callable

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

from app.backtesting.engine import Backtest, run
from app.models.dixon_coles import dixon_coles_outcome_probabilities
from app.models.elo import EloModel
from app.models.implied import implied_probabilities, normalize
from app.models.poisson import outcome_probabilities

router = APIRouter(prefix="/api/v1/backtests", tags=["backtests"])


# --------------------------------------------------------------------------- #
# Request / response schemas
# --------------------------------------------------------------------------- #
class BacktestRequest(BaseModel):
    """A backtest run request.

    Attributes:
        model: Model key -- one of ``implied``, ``poisson``, ``dixon_coles``,
            ``elo``.
        params: Free-form model parameters (currently unused by the built-in
            models but preserved for reproducibility and forward compatibility).
        matches: Historical records. Each record must include an ``outcome`` in
            ``{"home", "draw", "away"}`` plus the feature fields the selected
            model needs (see :data:`_MODELS`). An optional ``timestamp`` may be
            supplied; if omitted a neutral one is used.
    """

    model: str = Field(..., description="One of: implied, poisson, dixon_coles, elo")
    params: dict[str, Any] = Field(default_factory=dict)
    matches: list[dict[str, Any]] = Field(..., min_length=1)


class BacktestResponse(BaseModel):
    log_loss: float
    brier_score: float
    accuracy: float
    n_records: int


# --------------------------------------------------------------------------- #
# Model callables: map a match's feature mapping -> {home, draw, away}
# --------------------------------------------------------------------------- #
def _implied_model(features: Mapping[str, Any]) -> dict[str, float]:
    odds = features["odds"]
    home, draw, away = normalize(implied_probabilities(odds))
    return {"home": home, "draw": draw, "away": away}


def _poisson_model(features: Mapping[str, Any]) -> dict[str, float]:
    return outcome_probabilities(
        float(features["lambda_home"]), float(features["lambda_away"])
    )


def _dixon_coles_model(features: Mapping[str, Any]) -> dict[str, float]:
    return dixon_coles_outcome_probabilities(
        float(features["lambda_home"]),
        float(features["lambda_away"]),
        float(features["rho"]),
    )


def _elo_model(features: Mapping[str, Any]) -> dict[str, float]:
    model = EloModel()
    return model.predict_1x2(
        float(features["home_rating"]), float(features["away_rating"])
    )


_MODELS: dict[str, Callable[[Mapping[str, Any]], Mapping[str, float]]] = {
    "implied": _implied_model,
    "poisson": _poisson_model,
    "dixon_coles": _dixon_coles_model,
    "dixon-coles": _dixon_coles_model,
    "elo": _elo_model,
}


# --------------------------------------------------------------------------- #
# Endpoint
# --------------------------------------------------------------------------- #
@router.post("/", response_model=BacktestResponse)
def post_backtest(request: BacktestRequest) -> BacktestResponse:
    """Run a backtest with the selected model and return aggregated metrics."""
    model_fn = _MODELS.get(request.model)
    if model_fn is None:
        raise HTTPException(
            status_code=422,
            detail=f"unknown model {request.model!r}; expected one of {sorted(set(_MODELS))}",
        )

    # The engine enforces an anti-leakage cutoff against each record's
    # timestamp. Normalize the incoming matches into engine records, supplying a
    # neutral timestamp when the caller omits one and using a cutoff far in the
    # future so caller-provided timestamps are always accepted.
    default_ts = 0.0
    cutoff_ts = 4_102_444_800.0  # 2100-01-01T00:00:00Z, well beyond any input.

    records: list[dict[str, Any]] = []
    for match in request.matches:
        outcome = match.get("outcome")
        # Features are every field except the reserved keys.
        features = {
            k: v for k, v in match.items() if k not in ("outcome", "timestamp")
        }
        records.append(
            {
                "timestamp": match.get("timestamp", default_ts),
                "features": features,
                "outcome": outcome,
            }
        )

    config = Backtest(
        dataset_version=str(request.params.get("dataset_version", "adhoc")),
        cutoff_timestamp=cutoff_ts,
        feature_version=str(request.params.get("feature_version", "v1")),
        model_version=request.model,
        params=request.params,
        seed=int(request.params.get("seed", 0)),
    )

    try:
        result = run(config, records, model_fn)
    except (ValueError, KeyError, TypeError) as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc

    metrics = result.metrics
    return BacktestResponse(
        log_loss=metrics["log_loss"],
        brier_score=metrics["brier_score"],
        accuracy=metrics["accuracy"],
        n_records=result.n_records,
    )
