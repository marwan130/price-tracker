# Scraping workflow

## Services and ownership

- ASP.NET Core owns listings, job creation, input validation, persistence, product identity, pricing decisions, and user-facing job state.
- RabbitMQ carries versioned, durable messages on a private Compose network.
- The Python worker retrieves one requested HTTPS page and extracts product facts with Scrapling from Product JSON-LD, host-scoped retailer selectors, and page-level Open Graph image metadata. It does not infer currency or create database records.
- PostgreSQL is the source of truth. Scrape requests are committed to the outbox before publication; result effects and inbox deduplication are committed in one database transaction.

The installed library is pinned to `scrapling[fetchers]==0.5`. The worker uses `scrapling.fetchers.AsyncFetcher.get` and Scrapling's safe redirect behavior. See the [official Scrapling repository](https://github.com/D4Vinci/Scrapling) and [documentation](https://scrapling.readthedocs.io/en/latest/).

When JSON-LD includes multiple Product records or offers, extraction prefers the record with a valid explicit price, then valid currency, title, availability, and image; ties preserve source order. If the chosen product omits its image, the worker falls back to Open Graph, Twitter card, image-source, and product-image metadata. Relative image paths are resolved against the page URL. Worker results use the API contract's camel-case field names so image URLs survive deserialization. The worker does not infer currency or select a cheaper recommendation without the API's identity and market checks.

## Scrape job flow

Authenticated clients can request a refresh with `POST /v1/listings/{listingId}/scrape`. The API writes a `scrape.job.requested.v1` message to `OutboxMessages` and returns a job ID. The outbox publisher awaits RabbitMQ publisher confirms before marking a message published. `GET /v1/scrape-jobs/{jobId}` reports queued, completed, failed, or rejected state for the requesting user.

Message envelope (schema version 1):

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "correlationId": "uuid",
  "occurredAtUtc": "2026-11-02T12:00:00Z",
  "payload": { "jobId": "uuid", "listingId": "uuid", "url": "https://store.example/product" }
}
```

The worker publishes `scrape.result.completed.v1` or `scrape.result.failed.v1` on `dealio.results.v1`. Results include `jobId` and `listingId`. The API rejects malformed messages to the result dead-letter queue. It checks explicit positive price, configured currency, listing currency, and a conservative title match before writing a price observation. A price below half or above twice the last accepted price is fetched once more; a recheck outside five percent of the candidate is rejected and cannot trigger target alerts. A unique inbox logical key makes redelivered results idempotent. Rejected extraction results remain visible as completed job state with outcome `rejected` and do not alter price history.

## Reliability and operations

- Durable exchanges and queues; persistent messages; manual acknowledgements; bounded prefetch.
- Outbox rows survive broker outages. Publication retry uses capped exponential backoff with jitter.
- The worker has three bounded durable retry tiers (5 seconds, 30 seconds, 120 seconds). Exhausted jobs produce structured failures and enter the dead-letter flow.
- Work, retry, API result, and dead-letter queues are bounded to 10,000 messages with reject-publish overflow. Watch queue depth and publisher failures; tune limits with measured VPS capacity before production.
- Broker ports are not published by Compose. Only the API and worker connect on the internal network.
- Inbox records currently have no automatic retention job; plan and test retention before high-volume production.
- The worker validates DNS before fetch and Scrapling applies safe redirect handling. DNS rebinding defenses still depend on egress firewall policy; restrict outbound worker network access in deployment.

## Search verification and scheduled rediscovery

Search sessions are stored in PostgreSQL and replayed with `Last-Event-ID` (or a `sessionId` on reconnect); search work continues when the browser disconnects. Provider candidates are displayed as unverified first, then queued through the same durable outbox for Scrapling extraction. A candidate is marked verified only when title, positive price, configured currency, and market currency checks pass. Failed extraction produces a generic source failure event. Sessions remain available for 24 hours.

The daily `rediscover-tracked-products` job selects bounded batches of active, unpaused tracking product/market pairs and creates durable search sessions. A verified result can be attached as a new offer only if its normalized full title exactly matches the tracked product, the product has one active attribute-free variant, the currency matches the store market, and neither that URL nor a variant/store listing already exists. Variant ambiguity or a conflicting offer is left for review; no fuzzy cross-product merge is performed. Existing active offers continue to refresh on the hourly scheduled job.

The URL preview endpoint now creates a durable one-candidate session and sends the URL through the same Scrapling outbox and worker. It preserves the normal product-result response when verification finishes within 25 seconds; otherwise it returns a session URL for the frontend to resume over SSE. User-submitted URLs do not need a prior title match, while extracted price, configured currency, HTTPS, DNS, and non-public-address checks still apply. The API derives a supported market from known retailer hosts and locale paths for URL imports. Retailer access varies by retailer and environment; permanent client errors are not retried, while timeouts, rate limits, and server errors use bounded retry tiers.

When JSON-LD is absent, the worker has host-scoped fallback selectors for Jumia Egypt/Saudi Arabia, Noon, Amazon Egypt/Saudi Arabia/UAE/US, and Namshi. These extractors read price text and return currency only from explicit currency metadata; they never infer currency from a symbol or hostname. Offline fixtures cover Jumia, Noon, Amazon Egypt, and Namshi. Not every fallback selector has current live-page validation; re-check each store module against live pages before relying on it in production. Store health is exposed through the Admin-only `/v1/operations/store-health` endpoint using recent scrape logs; it reports healthy, degraded, unhealthy, stale, or unknown status but does not automatically suspend a store.

Ambiguous verified rediscovery candidates are placed in the Admin-only `/v1/operations/match-reviews` queue. Settings lets an admin link a candidate to an existing product or dismiss it. Accepting checks market currency and HTTPS, then saves the offer and initial price observation with the resolution event. Normal search sessions expire after their configured retention period; cleanup preserves sessions with unresolved match reviews until an admin resolves them.

The API rejects non-HTTPS and credentialed URL submissions. The Scrapling worker separately blocks literal and DNS-resolved non-public addresses before fetching. Python scraper fixtures run without network access:

```powershell
python -m pip install -r scraper/requirements.txt
$env:PYTHONPATH = (Join-Path $PWD 'scraper')
python -m pytest scraper/tests -q
```
