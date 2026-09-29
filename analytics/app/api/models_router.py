"""API router exposing the baseline quantitative models.

Prefix: ``/api/v1/models``

Endpoints:
- ``POST /implied``      -> implied + normalized probabilities + overround
- ``POST /poisson``      -> 1X2 + over/under probabilities
- ``POST /dixon-coles``  -> Dixon-Coles-adjusted 1X2 + over/under
- ``POST /elo/predict``  -> ELO 1X2 probabilities
"""

from __future__ import annotations

from fastapi import APIRouter
from pydantic import BaseModel, Field

from app.models.dixon_coles import (
    dixon_coles_outcome_probabilities,
    dixon_coles_over_under,
)
from app.models.elo import EloModel
from app.models.implied import implied_probabilities, normalize, overround
from app.models.poisson import outcome_probabilities, over_under

router = APIRouter(prefix="/api/v1/models", tags=["models"])


# --------------------------------------------------------------------------- #
# Request / response schemas
# --------------------------------------------------------------------------- #
class ImpliedRequest(BaseModel):
    odds: list[float] = Field(..., min_length=1, description="Decimal odds, each > 1.0")


class ImpliedResponse(BaseModel):
    implied: list[float]
    normalized: list[float]
    overround: float


class OneX2(BaseModel):
    home: float
    draw: float
    away: float


class OverUnder(BaseModel):
    line: float
    over: float
    under: float


class PoissonRequest(BaseModel):
    lambda_home: float = Field(..., ge=0.0)
    lambda_away: float = Field(..., ge=0.0)
    ou_line: float = Field(2.5, gt=0.0)


class PoissonResponse(BaseModel):
    one_x_two: OneX2
    over_under: OverUnder


class DixonColesRequest(BaseModel):
    lambda_home: float = Field(..., ge=0.0)
    lambda_away: float = Field(..., ge=0.0)
    rho: float = Field(..., description="Low-score dependence parameter")
    ou_line: float = Field(2.5, gt=0.0)


class DixonColesResponse(BaseModel):
    one_x_two: OneX2
    over_under: OverUnder


class EloPredictRequest(BaseModel):
    home_rating: float
    away_rating: float


class EloPredictResponse(BaseModel):
    one_x_two: OneX2


# --------------------------------------------------------------------------- #
# Endpoints
# --------------------------------------------------------------------------- #
@router.post("/implied", response_model=ImpliedResponse)
def post_implied(request: ImpliedRequest) -> ImpliedResponse:
    """Convert decimal odds to implied + normalized probabilities and overround."""
    implied = implied_probabilities(request.odds)
    return ImpliedResponse(
        implied=implied,
        normalized=normalize(implied),
        overround=overround(request.odds),
    )


@router.post("/poisson", response_model=PoissonResponse)
def post_poisson(request: PoissonRequest) -> PoissonResponse:
    """Independent-Poisson 1X2 + Over/Under probabilities."""
    probs = outcome_probabilities(request.lambda_home, request.lambda_away)
    ou = over_under(request.lambda_home, request.lambda_away, line=request.ou_line)
    return PoissonResponse(
        one_x_two=OneX2(**probs),
        over_under=OverUnder(line=request.ou_line, over=ou["over"], under=ou["under"]),
    )


@router.post("/dixon-coles", response_model=DixonColesResponse)
def post_dixon_coles(request: DixonColesRequest) -> DixonColesResponse:
    """Dixon-Coles-adjusted 1X2 + Over/Under probabilities."""
    probs = dixon_coles_outcome_probabilities(
        request.lambda_home, request.lambda_away, request.rho
    )
    ou = dixon_coles_over_under(
        request.lambda_home, request.lambda_away, request.rho, line=request.ou_line
    )
    return DixonColesResponse(
        one_x_two=OneX2(**probs),
        over_under=OverUnder(line=request.ou_line, over=ou["over"], under=ou["under"]),
    )


@router.post("/elo/predict", response_model=EloPredictResponse)
def post_elo_predict(request: EloPredictRequest) -> EloPredictResponse:
    """ELO 1X2 prediction using the default model configuration."""
    model = EloModel()
    probs = model.predict_1x2(request.home_rating, request.away_rating)
    return EloPredictResponse(one_x_two=OneX2(**probs))
