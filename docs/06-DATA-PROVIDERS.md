# 06 — Data Providers

## Provider abstraction
IDataProvider deve esporre operations per competitions, teams, standings, fixtures, match, statistics, odds e odds history.

## Adapter requirements
Authentication, rate limiting, retries, pagination, mapping, provenance, raw payload persistence ed error classification.

## Canonicalization
Conservare provider IDs insieme agli internal IDs. Team aliases e market aliases vengono normalizzati.

## Oddspedia
Un adapter Oddspedia può esistere solo quando accesso e riutilizzo sono autorizzati da termini/licenza applicabili. Il prodotto non deve dipendere da una singola fonte.
