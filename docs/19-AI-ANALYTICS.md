# 19 — AI Analytics

## Goal
Permettere query analitiche in linguaggio naturale mantenendo l'esecuzione deterministica.

## Architecture
User question → Intent parser → Metric resolver → Query planner → Analytics executor → Validator → Explanation.

## Guardrails
L'AI non può inventare numeri. Le query devono eseguire su dataset approvati. Risultati con timeframe, filtri, freshness e provenance.

## Example
Confrontare la forma delle squadre e i movimenti 1X2 nelle ultime 24 ore.

## Principle
AI spiega e interroga il data layer; non sostituisce la pipeline dati.
