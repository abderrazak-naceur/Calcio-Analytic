"""API router exposing the deterministic match-summary generator.

Prefix: ``/api/v1/summaries``

Endpoint:
- ``POST /match`` -> turn a stored ``MatchAnalysisReport``-shaped facts object
  into a strictly descriptive, non-predictive summary.

The endpoint is a thin wrapper over :func:`app.summaries.generator.summarize_match`.
It performs **no** fabrication: the response describes only the facts supplied in
the request body. The request model is intentionally permissive so it round-trips
the full report shape (including extra/unknown keys) without schema churn.
"""

from __future__ import annotations

from typing import Any

from fastapi import APIRouter
from pydantic import BaseModel, Field

from app.summaries.generator import summarize_match

router = APIRouter(prefix="/api/v1/summaries", tags=["summaries"])


# --------------------------------------------------------------------------- #
# Request / response schemas
# --------------------------------------------------------------------------- #
class MatchSummaryRequest(BaseModel):
    """A request to summarize a match-analysis report.

    Attributes:
        facts: The ``MatchAnalysisReport``-shaped object to describe. Kept as a
            free-form mapping so the endpoint is robust to the full nested shape
            and to extra keys; the summarizer reads only the fields it knows and
            ignores the rest.
    """

    facts: dict[str, Any] = Field(
        default_factory=dict,
        description="MatchAnalysisReport-shaped facts to describe (camelCase or snake_case).",
    )


class MatchSummaryResponse(BaseModel):
    summary: str
    bullets: list[str]
    dataCompleteness: dict[str, bool]
    caveats: list[str]


# --------------------------------------------------------------------------- #
# Endpoint
# --------------------------------------------------------------------------- #
@router.post("/match", response_model=MatchSummaryResponse)
def post_match_summary(request: MatchSummaryRequest) -> MatchSummaryResponse:
    """Return a descriptive, non-predictive summary of the supplied facts."""
    result = summarize_match(request.facts)
    return MatchSummaryResponse(**result)
