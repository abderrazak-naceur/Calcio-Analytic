# 22 — Testing Strategy

## Objective
Verify ingestion, historical correctness, settlement, analytics and APIs without allowing future information to leak into past analysis.

## Test layers

### Unit
- odds probability calculations
- overround
- normalized probabilities
- opening/closing selection
- movement calculations
- market settlement
- line parsing
- team-form windows
- H2H windows
- similar-match scoring
- pattern aggregation
- model metrics

### Integration
- provider adapter -> canonical DTO
- canonical DTO -> PostgreSQL
- duplicate snapshot handling
- match lifecycle
- post-match analysis
- API pagination/filtering
- Redis/cache behavior

### Contract
Every provider adapter gets fixtures representing:
- normal response
- missing market
- suspended selection
- changed bookmaker
- duplicate update
- malformed field
- timezone edge case

### End-to-end
Scenario:
1. ingest future fixture
2. ingest opening odds
3. ingest multiple odds changes
4. ingest live events/statistics
5. ingest final result
6. run post-match analysis
7. query match report
8. verify historical pattern includes the match

## Point-in-time tests
For every analytical feature, create a test proving that data captured after the analysis timestamp is ignored.

## Idempotency tests
Running the same provider payload twice must not create duplicate logical snapshots.

## Settlement tests
Use canonical fixtures for:
- 1X2
- double chance
- draw no bet
- over/under
- Asian handicap
- BTTS
- correct score
- half-time
- corners
- cards
- player markets where supported

Unsupported settlement must remain UNKNOWN rather than being guessed.

## Performance
Benchmark:
- odds ingestion throughput
- historical query latency
- match analysis generation
- pattern aggregation
- dashboard queries

## Data quality tests
Reject or quarantine:
- impossible odds
- invalid timestamps
- unknown mappings
- duplicate provider identifiers
- inconsistent match state
- impossible score/event sequences

## Acceptance
A feature is complete only when unit, integration and relevant end-to-end tests pass and historical reproducibility is demonstrated.
