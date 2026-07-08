# Price Tracker Scraper

Standalone worker that fetches active listings from the API, scrapes product pages for prices, and posts results back via internal endpoints.

## Prerequisites

- .NET 10 SDK
- Price Tracker API running locally or in staging
- `InternalApi:Key` matching the API configuration

## Configuration

`appsettings.json` holds defaults. Override locally with `appsettings.Development.json` (gitignored) or environment variables:

| Variable | Description |
|----------|-------------|
| `Api__BaseUrl` | API base URL (e.g. `http://localhost:5000`) |
| `Api__InternalKey` | Value for `X-Internal-Key` header |
| `Scraper__IntervalMinutes` | Minutes between scrape cycles (default `60`) |
| `Scraper__RequestTimeoutSeconds` | HTTP timeout for API and page requests |
| `Scraper__DelayBetweenListingsMs` | Delay between listing scrapes |

## Run

```bash
cd scraper/PriceTracker.Scraper
dotnet run
```

## API contract 

The scraper expects these internal endpoints on the backend:

| Method | Path | Auth |
|--------|------|------|
| `GET` | `/v1/internal/listings/active` | `X-Internal-Key` |
| `POST` | `/v1/price-history` | `X-Internal-Key` |
| `POST` | `/v1/scrape-logs` | `X-Internal-Key` |

## Price extraction

`HtmlPriceExtractor` tries, in order:

1. JSON-LD (`application/ld+json`) product offers
2. Open Graph / product meta tags
3. Common DOM selectors (`data-price`, `itemprop="price"`, `.price`)
4. Fallback numeric pattern in page text