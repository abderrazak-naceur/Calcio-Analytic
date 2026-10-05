# 35 — SportMonks Integration

## Role

SportMonks is the primary football data provider for Calcio-Analytic.

The integration must feed the Market Intelligence pipeline:

`SportMonks → fixtures/results → odds snapshots → settlement → Market vs Reality`.

## Implemented foundation

The current branch contains:

- `SportmonksProvider` implementing football fixtures, seasons, bookmakers, markets and pre-match odds;
- `SportmonksIngestionController` for leagues, seasons and season import;
- provider registration through dependency injection;
- Docker environment forwarding for `Sportmonks__ApiToken`;
- representative provider mapping tests;
- token validation with actionable configuration errors.

## Configuration

The token must be supplied through .NET configuration/User Secrets or deployment secrets:

`Sportmonks:ApiToken`

Never commit a real provider token into `.env.example`, source code, tests or frontend configuration.

If a credential has already appeared in repository history, removing it from the current file is not sufficient: the credential must be revoked/rotated at the provider.

## Odds handling

The adapter requests pre-match odds and preserves provider/bookmaker timestamps. The downstream Market Intelligence API applies the stricter pre-kickoff rule:

`ProviderTimestampUtc < Match.KickoffUtc`

Historical odds must remain append-only. No post-kickoff snapshot may become pre-match evidence.

## Validation still required

Before marking DATA-001 DONE:

1. configure a valid SportMonks token outside the repository;
2. verify subscribed league and season discovery;
3. import a representative season or fixture;
4. verify final scores and team identity mapping;
5. verify bookmaker and market coverage;
6. verify 1X2, Over/Under and BTTS availability where subscribed;
7. verify odds timestamps and pre-kickoff coverage;
8. measure historical odds depth and document provider limitations;
9. verify rate limits and retry/throttling behavior;
10. persist representative raw/provider payloads as tests without committing secrets.

## Product boundary

SportMonks is the primary source. Public web acquisition remains secondary and must only use permitted/licensed access. It must never be used to bypass authentication, CAPTCHA, anti-bot controls or provider restrictions.
