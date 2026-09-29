"""API tests for the summaries router using TestClient."""

from __future__ import annotations

from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)

_DESCRIPTIVE_CAVEAT = "Historical patterns are descriptive, not predictive."


def _full_facts() -> dict:
    return {
        "result": {"homeScore": 2, "awayScore": 1, "outcome": "HomeWin"},
        "markets": [{"marketLineId": "m1", "overround": 1.05, "marginPercentage": 0.05}],
        "oddsMovements": [
            {"movement": {"openingOdds": 2.10, "closingOdds": 1.90, "movementPercentage": -0.095}}
        ],
        "bookmakerDispersions": [
            {"dispersion": {"bookmakerCount": 3, "bestOdds": 2.05, "worstOdds": 1.85}}
        ],
        "statistics": {
            "statistics": [{"name": "Shots", "totalValue": 21}],
            "eventCounts": [{"type": "goal", "count": 3}],
            "totalEvents": 3,
        },
        "methodologyVersion": "v1.2.0",
    }


def test_match_summary_endpoint_returns_200_with_summary_and_bullets():
    resp = client.post("/api/v1/summaries/match", json={"facts": _full_facts()})

    assert resp.status_code == 200
    body = resp.json()
    assert isinstance(body["summary"], str) and body["summary"].strip()
    assert isinstance(body["bullets"], list) and len(body["bullets"]) > 0
    assert _DESCRIPTIVE_CAVEAT in body["caveats"]
    assert body["dataCompleteness"]["result"] is True


def test_match_summary_endpoint_missing_scores_reports_unavailable():
    facts = _full_facts()
    facts["result"] = {"homeScore": None, "awayScore": None, "outcome": "Unknown"}

    resp = client.post("/api/v1/summaries/match", json={"facts": facts})

    assert resp.status_code == 200
    body = resp.json()
    assert body["dataCompleteness"]["result"] is False
    assert any("not available" in b.lower() for b in body["bullets"])
    assert any(c.startswith("The following data was unavailable") for c in body["caveats"])


def test_match_summary_endpoint_empty_body_is_ok():
    resp = client.post("/api/v1/summaries/match", json={})

    assert resp.status_code == 200
    body = resp.json()
    assert _DESCRIPTIVE_CAVEAT in body["caveats"]
    assert body["summary"].strip()
