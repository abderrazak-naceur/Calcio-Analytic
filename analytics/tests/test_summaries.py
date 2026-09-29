"""Unit tests for the deterministic match-summary generator.

These tests pin the three load-bearing guarantees of the summarizer:
1. A full facts dict yields a non-empty summary + bullets and always carries the
   "descriptive, not predictive" caveat.
2. A facts dict with null scores yields a "not available" result sentence and
   lists the missing data in the caveats (no fabrication).
3. The function is deterministic: the same input maps to identical output.
"""

from __future__ import annotations

from app.summaries.generator import summarize_match

_DESCRIPTIVE_CAVEAT = "Historical patterns are descriptive, not predictive."


def _full_facts() -> dict:
    """A complete MatchAnalysisReport-shaped facts dict (camelCase, as serialized)."""
    return {
        "result": {"homeScore": 2, "awayScore": 1, "outcome": "HomeWin"},
        "markets": [
            {
                "marketLineId": "m1",
                "overround": 1.048,
                "marginPercentage": 0.048,
                "selections": [],
            }
        ],
        "oddsMovements": [
            {
                "selectionId": "s1",
                "movement": {
                    "openingOdds": 2.10,
                    "closingOdds": 1.90,
                    "movementPercentage": -0.095,
                },
            },
            {
                "selectionId": "s2",
                "movement": {
                    "openingOdds": 3.40,
                    "closingOdds": 3.50,
                    "movementPercentage": 0.029,
                },
            },
        ],
        "bookmakerDispersions": [
            {
                "selectionId": "s1",
                "dispersion": {
                    "bookmakerCount": 4,
                    "bestOdds": 2.05,
                    "worstOdds": 1.85,
                },
            }
        ],
        "statistics": {
            "statistics": [
                {"name": "Shots", "totalValue": 21},
                {"name": "Corners", "totalValue": 9},
            ],
            "eventCounts": [{"type": "goal", "count": 3}],
            "totalEvents": 3,
        },
        "methodologyVersion": "v1.2.0",
    }


def test_full_facts_produces_summary_bullets_and_descriptive_caveat():
    out = summarize_match(_full_facts())

    assert isinstance(out["summary"], str) and out["summary"].strip()
    assert isinstance(out["bullets"], list) and len(out["bullets"]) > 0
    # Standing caveat must always be present.
    assert _DESCRIPTIVE_CAVEAT in out["caveats"]

    # Every section was present, so completeness is all-True and nothing is
    # reported as missing.
    assert all(out["dataCompleteness"].values())
    assert not any(c.startswith("The following data was unavailable") for c in out["caveats"])


def test_full_facts_result_and_movement_are_factual_past_tense():
    out = summarize_match(_full_facts())
    summary = out["summary"]

    # Result traceable to score + outcome.
    assert "2-1" in summary
    assert "home win" in summary

    # Largest absolute movement is -0.095 (shortened by 9.5%), not the +0.029 one.
    assert "shortened" in summary
    assert "9.5%" in summary
    # No predictive framing.
    lowered = summary.lower()
    for banned in ("will", "expected to", "predict", "likely to win"):
        assert banned not in lowered


def test_null_scores_yield_not_available_and_missing_caveat():
    facts = _full_facts()
    facts["result"] = {"homeScore": None, "awayScore": None, "outcome": "Unknown"}

    out = summarize_match(facts)

    # Result sentence reports unavailability rather than inventing a scoreline.
    assert any("not available" in b.lower() for b in out["bullets"])
    assert out["dataCompleteness"]["result"] is False

    # The missing data is explicitly listed in the caveats.
    missing_caveats = [c for c in out["caveats"] if c.startswith("The following data was unavailable")]
    assert len(missing_caveats) == 1
    assert "final result" in missing_caveats[0]
    # Standing caveat still present.
    assert _DESCRIPTIVE_CAVEAT in out["caveats"]


def test_empty_facts_lists_everything_missing_and_never_fabricates():
    out = summarize_match({})

    assert out["dataCompleteness"] == {
        "result": False,
        "oddsMovements": False,
        "markets": False,
        "bookmakerDispersions": False,
        "statistics": False,
    }
    assert _DESCRIPTIVE_CAVEAT in out["caveats"]
    missing_caveats = [c for c in out["caveats"] if c.startswith("The following data was unavailable")]
    assert len(missing_caveats) == 1
    for label in ("final result", "odds movements", "market overround", "bookmaker dispersion", "match statistics"):
        assert label in missing_caveats[0]


def test_none_input_is_tolerated():
    out = summarize_match(None)  # type: ignore[arg-type]
    assert isinstance(out["summary"], str) and out["summary"].strip()
    assert _DESCRIPTIVE_CAVEAT in out["caveats"]


def test_deterministic_same_input_identical_output():
    facts = _full_facts()
    first = summarize_match(facts)
    second = summarize_match(_full_facts())
    assert first == second

    # Repeated calls on the very same object are also stable.
    assert summarize_match(facts) == summarize_match(facts)


def test_statistics_and_market_sentences_are_traceable():
    out = summarize_match(_full_facts())
    summary = out["summary"]

    # Statistics traceable to totalEvents + named stats.
    assert "3 recorded event(s)" in summary
    assert "Shots: 21" in summary
    # Market overround / margin traceable.
    assert "overround of 1.048" in summary
    # Dispersion best/worst traceable.
    assert "best of 2.05" in summary
    assert "worst of 1.85" in summary
