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

from app.config import settings
from app.summaries.generator import summarize_match
from app.summaries.ai_provider import generate_ai_summary

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
    provider: str = "deterministic"
    model: str | None = None


# --------------------------------------------------------------------------- #
# Endpoint
# --------------------------------------------------------------------------- #
@router.post("/match", response_model=MatchSummaryResponse)
def post_match_summary(request: MatchSummaryRequest) -> MatchSummaryResponse:
    """Return a descriptive, non-predictive summary of the supplied facts."""
    result = generate_ai_summary(request.facts)
    provider = "openai-compatible"
    model = None
    if result is None:
        result = summarize_match(request.facts)
        provider = "deterministic"
    else:
        model = settings.ai_summary_model
    result["provider"] = provider
    result["model"] = model
    return MatchSummaryResponse(**result)
