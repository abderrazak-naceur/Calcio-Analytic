"""Optional provider-neutral LLM summarizer with deterministic fallback."""

from __future__ import annotations

import json
import urllib.error
import urllib.request
from typing import Any

from app.config import settings


def _extract_json(text: str) -> dict[str, Any] | None:
    candidate = text.strip()
    if candidate.startswith("```"):
        candidate = candidate.split("\n", 1)[1] if "\n" in candidate else candidate[3:]
        if candidate.endswith("```"):
            candidate = candidate[:-3].strip()
    try:
        value = json.loads(candidate)
    except json.JSONDecodeError:
        return None
    return value if isinstance(value, dict) else None


def generate_ai_summary(facts: dict[str, Any]) -> dict[str, Any] | None:
    """Generate a structured descriptive summary, or return None on failure."""
    if not settings.ai_summary_enabled or not settings.ai_summary_base_url or not settings.ai_summary_model:
        return None

    system_prompt = (
        "You are a football analytics summarizer. Describe only the supplied facts. "
        "Never predict a future result, invent a statistic, infer missing data, or recommend a bet. "
        "Return JSON only with keys summary, bullets, dataCompleteness, and caveats. "
        "Always include this exact caveat: Historical patterns are descriptive, not predictive."
    )
    payload = {
        "model": settings.ai_summary_model,
        "temperature": 0,
        "response_format": {"type": "json_object"},
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": json.dumps(facts, ensure_ascii=False, separators=(",", ":"))},
        ],
    }
    headers = {"Content-Type": "application/json"}
    if settings.ai_summary_api_key:
        headers["Authorization"] = f"Bearer {settings.ai_summary_api_key}"
    request = urllib.request.Request(
        settings.ai_summary_base_url.rstrip("/") + "/chat/completions",
        data=json.dumps(payload).encode("utf-8"), headers=headers, method="POST")

    try:
        with urllib.request.urlopen(request, timeout=settings.ai_summary_timeout_seconds) as response:
            body = json.loads(response.read().decode("utf-8"))
        result = _extract_json(body["choices"][0]["message"]["content"])
        if not result or not isinstance(result.get("summary"), str):
            return None
        if not isinstance(result.get("bullets"), list) or not isinstance(result.get("caveats"), list):
            return None
        if not isinstance(result.get("dataCompleteness"), dict):
            return None
        caveat = "Historical patterns are descriptive, not predictive."
        if caveat not in result["caveats"]:
            result["caveats"].append(caveat)
        return result
    except (urllib.error.URLError, TimeoutError, KeyError, IndexError, ValueError, TypeError):
        return None
