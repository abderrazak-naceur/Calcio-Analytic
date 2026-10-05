# Web Data Acquisition

Calcio-Analytic can acquire football data through interchangeable sources. The ingestion layer must not depend on a paid API.

## Strategy

1. Prefer licensed/public APIs when they are available and economical.
2. A compliant web scraper can be used when a public source permits automated access.
3. Keep source adapters isolated behind the ingestion/provider abstractions.
4. Store raw responses and normalized records separately so historical data can be reprocessed.
5. Capture retrieval timestamps so point-in-time analytics never confuse publication time with kickoff time.
6. Apply per-source throttling, retries, exponential backoff and idempotent persistence.
7. Validate the source's Terms of Service, robots directives, access restrictions and data licensing before enabling a production source.
8. Never attempt to bypass CAPTCHA, authentication, anti-bot controls or other technical access restrictions.

## Current implementation

The ingestion project now contains a generic `IScraperSource` and an HTTP implementation with:
- configurable timeout;
- minimum inter-request delay;
- retry/backoff for transient failures;
- response timestamp, ETag and Last-Modified capture;
- dependency-injection registration.

This is intentionally source-neutral. No site-specific scraper is enabled yet.

## Next implementation

For the first real source:
- validate legal/technical access;
- implement a source adapter/parser;
- map matches, teams, results and odds into the existing provider contracts;
- persist raw snapshots before normalization;
- add representative fixture tests;
- schedule incremental collection;
- monitor parser failures and source schema changes.

A paid provider remains a fallback for fields or historical depth that cannot be obtained reliably from permitted public sources.
