"""Deterministic descriptive summarizer over stored match-analysis facts.

The single public entry point is :func:`summarize_match`. It takes a facts dict
whose shape mirrors the .NET ``MatchAnalysisReport`` record (serialized to JSON)
and returns a plain-language, strictly *descriptive* summary.

Design rules (from the project spec) -- these are load-bearing, not stylistic:

1. **No fabrication.** Every sentence must be traceable to a field that was
   present in the input. If a fact is missing, the summary says it is
   unavailable rather than guessing or filling in a plausible value.
2. **Descriptive, not predictive.** Historical patterns (odds movements,
   dispersion, statistics) are reported in the past tense as observations. The
   summary never implies a future outcome. A standing caveat makes this explicit.
3. **Deterministic.** No randomness, no external calls (no LLM). The same input
   always yields byte-identical output. Iteration order over inputs is preserved
   as given; any internal selection (e.g. "largest movement") uses a stable,
   fully-specified tie-break.

The input is intentionally tolerated in either camelCase (as serialized by the
.NET service) or snake_case, and extra/unknown keys are ignored.
"""

from __future__ import annotations

from typing import Any, Optional

# The standing caveat that must always be present: it states the descriptive
# (non-predictive) nature of everything the summary reports.
_DESCRIPTIVE_CAVEAT = "Historical patterns are descriptive, not predictive."


# --------------------------------------------------------------------------- #
# Small, side-effect-free helpers
# --------------------------------------------------------------------------- #
def _get(mapping: Any, *names: str) -> Any:
    """Return the first present key from ``names`` in ``mapping``.

    Tolerates both camelCase and snake_case spellings and returns ``None`` when
    ``mapping`` is not a dict or none of the names are present. A key that is
    present with a ``None`` value is treated as present (returns ``None``),
    matching the "field exists but is null" case from the source record.
    """
    if not isinstance(mapping, dict):
        return None
    for name in names:
        if name in mapping:
            return mapping[name]
    return None


def _has_key(mapping: Any, *names: str) -> bool:
    """True if any of ``names`` is a key in ``mapping`` (even if its value is None)."""
    if not isinstance(mapping, dict):
        return False
    return any(name in mapping for name in names)


def _as_list(value: Any) -> list[Any]:
    """Return ``value`` as a list, or an empty list when it is absent/not a list."""
    return value if isinstance(value, list) else []


def _to_number(value: Any) -> Optional[float]:
    """Best-effort numeric coercion; returns ``None`` for non-numeric/absent input."""
    if isinstance(value, bool):  # bool is an int subclass; never treat as a number here
        return None
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, str):
        try:
            return float(value.strip())
        except ValueError:
            return None
    return None


def _fmt_pct(fraction: float) -> str:
    """Format a *fraction* (e.g. 0.037) as a trimmed percentage string ("3.7%")."""
    pct = fraction * 100.0
    text = f"{pct:.2f}".rstrip("0").rstrip(".")
    return f"{text}%"


def _fmt_num(value: float) -> str:
    """Format a numeric value without a trailing ".0" for whole numbers."""
    if value == int(value):
        return str(int(value))
    text = f"{value:.4f}".rstrip("0").rstrip(".")
    return text


# --------------------------------------------------------------------------- #
# Section builders -- each returns a sentence (or None) plus records what it saw
# --------------------------------------------------------------------------- #
def _result_sentence(facts: dict) -> tuple[Optional[str], bool]:
    """Describe the final score and 1X2 outcome.

    Returns ``(sentence, available)``. When scores are null/absent the sentence
    is the fixed "Result not available." string and ``available`` is False.
    """
    result = _get(facts, "result", "Result")
    home = _to_number(_get(result, "homeScore", "HomeScore", "home_score"))
    away = _to_number(_get(result, "awayScore", "AwayScore", "away_score"))
    outcome_raw = _get(result, "outcome", "Outcome")

    if home is None or away is None:
        return "Result not available (no final score recorded).", False

    hs, as_ = int(home), int(away)
    outcome_text = _describe_outcome(outcome_raw, hs, as_)
    return (
        f"The match finished {hs}-{as_} ({outcome_text}).",
        True,
    )


def _describe_outcome(outcome_raw: Any, home_score: int, away_score: int) -> str:
    """Map a stored outcome value to descriptive text, else derive from the score.

    Only reports what the outcome field says; if the field is missing/unknown we
    fall back to the score itself (still an input-traceable fact, not a guess).
    """
    normalized = _normalize_outcome(outcome_raw)
    if normalized == "home":
        return "home win"
    if normalized == "away":
        return "away win"
    if normalized == "draw":
        return "draw"
    # Unknown/absent stored outcome: describe strictly from the recorded score.
    if home_score > away_score:
        return "home win"
    if away_score > home_score:
        return "away win"
    return "draw"


def _normalize_outcome(outcome_raw: Any) -> Optional[str]:
    """Normalize the many spellings of the 1X2 outcome to home/draw/away/None."""
    if outcome_raw is None:
        return None
    text = str(outcome_raw).strip().lower()
    if text in ("home", "homewin", "home_win", "1"):
        return "home"
    if text in ("away", "awaywin", "away_win", "2"):
        return "away"
    if text in ("draw", "x"):
        return "draw"
    return None


def _odds_movement_sentence(facts: dict) -> Optional[str]:
    """Describe the single largest absolute price movement, in the past tense.

    Selection is deterministic: the entry with the greatest
    ``abs(movementPercentage)``; ties break toward the earliest such entry in
    input order. Reports the observed move only -- no forecast.
    """
    movements = _as_list(_get(facts, "oddsMovements", "OddsMovements", "odds_movements"))
    best_pct: Optional[float] = None
    best_entry: Optional[dict] = None

    for entry in movements:
        movement = _get(entry, "movement", "Movement") if _has_key(entry, "movement", "Movement") else entry
        pct = _to_number(_get(movement, "movementPercentage", "MovementPercentage", "movement_percentage"))
        if pct is None:
            continue
        if best_pct is None or abs(pct) > abs(best_pct):
            best_pct = pct
            best_entry = movement

    if best_pct is None or best_entry is None:
        return None

    # "shortened" = price fell (backers got worse value); "drifted" = price rose.
    if best_pct < 0:
        direction = "shortened"
    elif best_pct > 0:
        direction = "drifted"
    else:
        direction = "did not move"

    opening = _to_number(_get(best_entry, "openingOdds", "OpeningOdds", "opening_odds"))
    closing = _to_number(_get(best_entry, "closingOdds", "ClosingOdds", "closing_odds"))

    magnitude = _fmt_pct(abs(best_pct))
    if direction == "did not move":
        base = "The largest tracked price did not move over the recorded snapshots."
    else:
        base = f"The largest tracked price {direction} by {magnitude} from open to close."

    if opening is not None and closing is not None:
        base += f" (opened {_fmt_num(opening)}, closed {_fmt_num(closing)})."
    return base


def _market_sentence(facts: dict) -> Optional[str]:
    """Report the overround / bookmaker margin of the first available market."""
    markets = _as_list(_get(facts, "markets", "Markets"))
    for market in markets:
        overround = _to_number(_get(market, "overround", "Overround"))
        margin = _to_number(_get(market, "marginPercentage", "MarginPercentage", "margin_percentage"))
        if overround is None and margin is None:
            continue
        parts: list[str] = []
        if overround is not None:
            parts.append(f"an overround of {_fmt_num(overround)}")
        if margin is not None:
            parts.append(f"a bookmaker margin of {_fmt_pct(margin)}")
        return "The market carried " + " and ".join(parts) + "."
    return None


def _dispersion_sentence(facts: dict) -> Optional[str]:
    """Report the best/worst cross-bookmaker spread of the first available entry."""
    dispersions = _as_list(
        _get(facts, "bookmakerDispersions", "BookmakerDispersions", "bookmaker_dispersions")
    )
    for entry in dispersions:
        dispersion = (
            _get(entry, "dispersion", "Dispersion")
            if _has_key(entry, "dispersion", "Dispersion")
            else entry
        )
        best = _to_number(_get(dispersion, "bestOdds", "BestOdds", "best_odds"))
        worst = _to_number(_get(dispersion, "worstOdds", "WorstOdds", "worst_odds"))
        count = _to_number(_get(dispersion, "bookmakerCount", "BookmakerCount", "bookmaker_count"))
        if best is None or worst is None:
            continue
        sentence = (
            f"Across bookmakers the price ranged from a best of {_fmt_num(best)} "
            f"to a worst of {_fmt_num(worst)}"
        )
        if count is not None and int(count) > 0:
            sentence += f" over {int(count)} bookmaker(s)"
        return sentence + "."
    return None


def _statistics_sentence(facts: dict) -> Optional[str]:
    """Report the total event count and any named aggregated statistics."""
    stats = _get(facts, "statistics", "Statistics")
    if not isinstance(stats, dict):
        return None

    total_events = _to_number(_get(stats, "totalEvents", "TotalEvents", "total_events"))
    named = _as_list(_get(stats, "statistics", "Statistics"))

    parts: list[str] = []
    if total_events is not None:
        parts.append(f"{int(total_events)} recorded event(s)")

    named_bits: list[str] = []
    for item in named:
        name = _get(item, "name", "Name")
        value = _to_number(_get(item, "totalValue", "TotalValue", "total_value"))
        if name is None or value is None:
            continue
        named_bits.append(f"{name}: {_fmt_num(value)}")
    if named_bits:
        parts.append("aggregated " + ", ".join(named_bits))

    if not parts:
        return None
    return "Match data included " + "; ".join(parts) + "."


# --------------------------------------------------------------------------- #
# Public entry point
# --------------------------------------------------------------------------- #
def summarize_match(facts: dict) -> dict:
    """Produce a strictly descriptive summary of a match-analysis facts dict.

    Args:
        facts: A ``MatchAnalysisReport``-shaped mapping. Keys may be camelCase
            (as serialized by the .NET service) or snake_case; extra keys are
            ignored. ``None`` is tolerated and treated as an empty report.

    Returns:
        A dict with:
          - ``summary``: a single paragraph built only from present fields.
          - ``bullets``: the same facts as individual bullet strings.
          - ``dataCompleteness``: a bool per section indicating what was present.
          - ``caveats``: the standing descriptive-not-predictive caveat plus an
            explicit list of any sections that were unavailable.

    The function is pure and deterministic: no randomness and no I/O.
    """
    if not isinstance(facts, dict):
        facts = {}

    bullets: list[str] = []

    # Result -----------------------------------------------------------------
    result_text, result_available = _result_sentence(facts)
    if result_text:
        bullets.append(result_text)

    # Odds movement ----------------------------------------------------------
    movement_text = _odds_movement_sentence(facts)
    movement_available = movement_text is not None
    if movement_text:
        bullets.append(movement_text)

    # Market overround -------------------------------------------------------
    market_text = _market_sentence(facts)
    market_available = market_text is not None
    if market_text:
        bullets.append(market_text)

    # Bookmaker dispersion ---------------------------------------------------
    dispersion_text = _dispersion_sentence(facts)
    dispersion_available = dispersion_text is not None
    if dispersion_text:
        bullets.append(dispersion_text)

    # Statistics -------------------------------------------------------------
    statistics_text = _statistics_sentence(facts)
    statistics_available = statistics_text is not None
    if statistics_text:
        bullets.append(statistics_text)

    # Methodology (metadata, not an analytical claim) ------------------------
    methodology = _get(facts, "methodologyVersion", "MethodologyVersion", "methodology_version")
    if methodology is not None and str(methodology).strip():
        bullets.append(f"Analysis methodology version: {methodology}.")

    data_completeness = {
        "result": result_available,
        "oddsMovements": movement_available,
        "markets": market_available,
        "bookmakerDispersions": dispersion_available,
        "statistics": statistics_available,
    }

    # Caveats: always the standing descriptive caveat, then explicit unavailable
    # notes so a reader never mistakes an omission for an absence of the event.
    caveats: list[str] = [_DESCRIPTIVE_CAVEAT]
    missing_labels = {
        "result": "final result",
        "oddsMovements": "odds movements",
        "markets": "market overround",
        "bookmakerDispersions": "bookmaker dispersion",
        "statistics": "match statistics",
    }
    missing = [label for key, label in missing_labels.items() if not data_completeness[key]]
    if missing:
        caveats.append("The following data was unavailable: " + ", ".join(missing) + ".")

    if bullets:
        summary = " ".join(bullets)
    else:
        summary = "No analytical facts were provided for this match."

    return {
        "summary": summary,
        "bullets": bullets,
        "dataCompleteness": data_completeness,
        "caveats": caveats,
    }
