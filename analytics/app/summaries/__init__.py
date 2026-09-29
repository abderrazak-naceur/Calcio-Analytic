"""Deterministic, template/NLG-style match-summary generation.

This package turns a stored :class:`MatchAnalysisReport`-shaped facts dict into a
human-readable, strictly descriptive summary. It never invents statistics and
never presents historical/descriptive patterns as predictions -- every sentence
it emits is traceable to a field that was present in the input.
"""

from app.summaries.generator import summarize_match

__all__ = ["summarize_match"]
