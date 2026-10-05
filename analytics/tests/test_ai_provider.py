from app.summaries.ai_provider import _extract_json


def test_extract_json_accepts_plain_json():
    result = _extract_json('{"summary":"ok","bullets":[],"dataCompleteness":{},"caveats":[]}')
    assert result is not None
    assert result["summary"] == "ok"


def test_extract_json_accepts_markdown_fence():
    result = _extract_json('```json\n{"summary":"ok","bullets":[],"dataCompleteness":{},"caveats":[]}\n```')
    assert result is not None
    assert result["summary"] == "ok"
