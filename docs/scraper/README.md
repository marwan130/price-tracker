# Scraper README

The scraper is a standalone .NET worker. It requests active listings from the backend, scrapes each listing URL, and posts price records plus scrape logs through internal endpoints.

## Structure

```text
scraper/
  PriceTracker.Scraper/
    Api/            Backend API client and DTOs
    Configuration/  API and scraper options
    Scraping/       Store scraper implementations and factory
    Workers/        Background scrape cycle
```

## Flow

1. Fetch active listings from `/v1/internal/listings/active` using `X-Internal-Key`.
2. Page through active listings using `page` and `size`.
3. Delay between listing requests to reduce store/IP pressure.
4. Scrape price and currency from JSON-LD, meta tags, DOM price attributes, or text.
5. Reject invalid, zero, CAPTCHA-blocked, or unsupported results.
6. Post price records to `/v1/price-history`.
7. Post scrape logs to `/v1/scrape-logs`.

## Configuration

| Key | Purpose |
| --- | --- |
| `Api:BaseUrl` | Backend API base URL |
| `Api:InternalKey` | Value sent as `X-Internal-Key` |
| `Scraper:IntervalMinutes` | Delay between full scrape cycles |
| `Scraper:RequestTimeoutSeconds` | HTTP timeout |
| `Scraper:DelayBetweenListingsMs` | Delay plus jitter between listing scrapes |
| `Scraper:ListingPageSize` | Active-listing page size, clamped by backend |

Environment variable example:

```text
Api__BaseUrl=http://localhost:5000
Api__InternalKey=replace-me
Scraper__ListingPageSize=100
```

## Run

```bash
cd scraper/PriceTracker.Scraper
dotnet run
```

## Rate Limiting And Reliability

- Listings are fetched in bounded pages.
- Scraping runs sequentially with configured delay and jitter to reduce IP rate-limit pressure.
- Transient scrape failures retry with exponential backoff.
- Backend `429` write responses are retried once after `Retry-After` when available.

## Data Quality

The scraper only returns data parsed from the target page. It does not fabricate product names, images, or prices when extraction fails.

## Verification

```bash
dotnet build backend/PriceTracker.slnx
```