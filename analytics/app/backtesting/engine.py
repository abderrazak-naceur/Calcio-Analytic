"""Minimal, leakage-safe backtest runner.

Anti-leakage contract
----------------------
Point-in-time correctness is the caller's *and* the engine's responsibility:

1. The caller passes only records that were knowable at or before
   ``cutoff_timestamp`` (already point-in-time filtered). Each record carries a
   ``timestamp`` (ISO-8601 string or epoch seconds).
2. The engine *asserts* the contract: if any record's timestamp exceeds the
   cutoff it raises immediately rather than silently using future data.
3. The model callable receives only a record's ``features`` mapping -- never
   the ``outcome`` -- so the actual result cannot leak into the forecast.

A :class:`Backtest` captures the full reproducibility context (dataset,
feature, and model versions, params, and RNG seed) so a run can be replayed.
"""

from __future__ import annotations

from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Any

from app.backtesting.metrics import accuracy, brier_score, log_loss

# A model maps a feature mapping to a {home, draw, away} probability dict.
ModelFn = Callable[[Mapping[str, Any]], Mapping[str, float]]

_OUTCOME_TO_INDEX = {"home": 0, "draw": 1, "away": 2}


def _to_epoch(ts: Any) -> float:
    """Normalize an ISO-8601 string or numeric timestamp to epoch seconds."""
    if isinstance(ts, (int, float)):
        return float(ts)
    if isinstance(ts, str):
        text = ts.replace("Z", "+00:00")
        dt = datetime.fromisoformat(text)
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=timezone.utc)
        return dt.timestamp()
    raise TypeError(f"unsupported timestamp type: {type(ts)!r}")


@dataclass
class Backtest:
    """Reproducibility context for a backtest run.

    Attributes:
        dataset_version: Identifier of the dataset snapshot used.
        cutoff_timestamp: ISO-8601 string or epoch seconds. No record may have
            a timestamp after this value.
        feature_version: Identifier of the feature-engineering version.
        model_version: Identifier of the model version.
        params: Free-form model/run parameters.
        seed: RNG seed for reproducibility.
    """

    dataset_version: str
    cutoff_timestamp: str | float
    feature_version: str
    model_version: str
    params: dict[str, Any] = field(default_factory=dict)
    seed: int = 0

    @property
    def cutoff_epoch(self) -> float:
        return _to_epoch(self.cutoff_timestamp)


@dataclass
class BacktestResult:
    """Output of a backtest run.

    Attributes:
        predictions: Per-record forecasts, each ``{home, draw, away}``.
        metrics: Aggregated metrics over all records.
        n_records: Number of records evaluated.
        config: The :class:`Backtest` that produced this result.
    """

    predictions: list[dict[str, float]]
    metrics: dict[str, float]
    n_records: int
    config: Backtest


def run(
    config: Backtest,
    matches: Sequence[Mapping[str, Any]],
    model: ModelFn,
) -> BacktestResult:
    """Run a leakage-safe backtest.

    Each match record must contain:
    - ``timestamp``: when the match/observation occurred.
    - ``features``: a mapping passed to the model (never includes the outcome).
    - ``outcome``: the actual 1X2 result, either the string ``"home"``,
      ``"draw"``, ``"away"`` or the integer label ``0/1/2``.

    Args:
        config: Reproducibility context; provides the anti-leakage cutoff.
        matches: Point-in-time-filtered historical records.
        model: Callable producing ``{home, draw, away}`` probabilities from a
            record's ``features`` mapping.

    Returns:
        A :class:`BacktestResult` with per-record predictions and aggregated
        ``log_loss``, ``brier_score`` and ``accuracy`` metrics.

    Raises:
        ValueError: If ``matches`` is empty, a record is missing required keys,
            an outcome label is invalid, or the anti-leakage contract is
            violated (a record timestamp after the cutoff).
    """
    if not matches:
        raise ValueError("matches must be a non-empty sequence")

    cutoff = config.cutoff_epoch

    probs_matrix: list[list[float]] = []
    outcomes: list[int] = []
    predictions: list[dict[str, float]] = []

    for idx, record in enumerate(matches):
        if "timestamp" not in record:
            raise ValueError(f"record {idx} is missing 'timestamp'")
        if "features" not in record:
            raise ValueError(f"record {idx} is missing 'features'")
        if "outcome" not in record:
            raise ValueError(f"record {idx} is missing 'outcome'")

        ts = _to_epoch(record["timestamp"])
        if ts > cutoff:
            raise ValueError(
                f"anti-leakage violation: record {idx} timestamp {record['timestamp']!r} "
                f"is after cutoff {config.cutoff_timestamp!r}"
            )

        raw_outcome = record["outcome"]
        if isinstance(raw_outcome, str):
            if raw_outcome not in _OUTCOME_TO_INDEX:
                raise ValueError(f"record {idx} has invalid outcome {raw_outcome!r}")
            label = _OUTCOME_TO_INDEX[raw_outcome]
        elif isinstance(raw_outcome, int) and raw_outcome in (0, 1, 2):
            label = raw_outcome
        else:
            raise ValueError(f"record {idx} has invalid outcome {raw_outcome!r}")

        forecast = model(record["features"])
        prediction = {
            "home": float(forecast["home"]),
            "draw": float(forecast["draw"]),
            "away": float(forecast["away"]),
        }
        predictions.append(prediction)
        probs_matrix.append([prediction["home"], prediction["draw"], prediction["away"]])
        outcomes.append(label)

    metrics = {
        "log_loss": log_loss(probs_matrix, outcomes),
        "brier_score": brier_score(probs_matrix, outcomes),
        "accuracy": accuracy(probs_matrix, outcomes),
    }

    return BacktestResult(
        predictions=predictions,
        metrics=metrics,
        n_records=len(matches),
        config=config,
    )
