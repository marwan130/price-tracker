# Server and API

The API is an ASP.NET Core 10 application using PostgreSQL and EF Core. Domain decisions and persistence stay in the API; the Python worker only retrieves a requested URL and extracts page facts.

## Services

- `Dealio.API`: HTTP endpoints, authentication, health checks, Hangfire recurring alert evaluation, RabbitMQ outbox publisher/result consumer.
- `Dealio.Application`: product, tracking, notification, and authentication services, DTOs, validators, repository and queue interfaces.
- `Dealio.Domain`: user, product, variant, store offer, price history, tracking, notification, and messaging entities.
- `Dealio.Infrastructure`: PostgreSQL repositories/migrations, JWT and refresh-token storage, SMTP email, and persistence configuration.
- `Dealio.Tests`: unit tests plus PostgreSQL Testcontainers integration tests.

## API access

The API uses authorization by default. Registration, login, refresh, verification/reset links, product reads, search, and health checks are anonymous. Catalog mutation is restricted to administrators. User tracking and notification operations are scoped to the authenticated user. Scraper write endpoints require the internal API key.

Main route groups include `/v1/auth`, `/v1/products`, `/v1/products/search`, `/v1/tracking`, `/v1/listings`, `/v1/listings/{listingId}/scrape`, `/v1/scrape-jobs/{jobId}`, `/v1/notifications`, and `/v1/price-history`. Product images load directly from validated HTTP(S) retailer image URLs in the browser; the API does not proxy or process them. Development exposes Swagger at `/swagger`. `/health` is liveness; `/health/ready` checks service readiness including PostgreSQL.

## Durable scrape workflow

1. Tracking creation/reactivation stages scrape requests in the same PostgreSQL transaction as the tracking row. Manual refresh requests also write to `OutboxMessages`.
2. `RabbitMqOutboxWorker` publishes persistent, versioned JSON messages to `dealio.work.v1`, using publisher confirms and retry timestamps for failures.
3. The Python worker consumes `scrape.jobs.v1`, extracts JSON-LD Product data through Scrapling, and publishes a result. Malformed messages go to a dead-letter queue; transient failures use delayed retry queues with bounded jitter.
4. The API result consumer validates job/listing identity, positive price, explicit ISO currency, configured store currency, and conservative title similarity. Accepted observations are persisted, then inbox/message ids prevent duplicate effects. Target evaluation follows commit.
5. A scrape job can be checked by its owning user. Results can be `completed`, `rejected`, or `failed`.

Hangfire evaluates price alerts every 15 minutes and queues up to 200 stale active offers per hour. The scheduled refresh includes only offers tied to active, unpaused accounts; it skips inactive or excluded listings, ignores observations newer than six hours, and enforces a four-hour per-offer queue cooldown. A database timestamp prevents repeat queueing after worker restarts. A separate bounded daily job creates resumable search sessions for actively tracked product/market pairs; only verified exact-title matches with compatible market/currency are attached automatically.

The API stores outbox/inbox state in PostgreSQL. RabbitMQ has durable queues, bounded queue sizes, dead-letter routes, and persistent messages. PostgreSQL and RabbitMQ have persistent Compose volumes; do not remove them to troubleshoot.

## Notifications

Each tracking target stores its original amount and preferred target currency. Target evaluation converts an offer's original observed amount into that currency using a fresh rate snapshot before deciding whether it reached the target; if a cross-currency rate is unavailable, the evaluator waits rather than making a guessed comparison. The persisted notification retains the source amount/currency, converted trigger amount, conversion rate, rate timestamp, and original target value/currency. A later strictly lower source price for that same offer can create a further-decrease event. Repeated history evaluations are deduplicated, email preferences are distinct per event, and an email cooldown suppresses repetitive deliveries. In-app events are persisted independently of email sending. Email uses Brevo's transactional API when configured; otherwise the local SMTP fallback is selected. The Brevo integration uses the documented [`POST /v3/smtp/email` endpoint](https://developers.brevo.com/reference/send-transac-email). Real Brevo delivery still needs an account-backed release check.

## Configuration

Use environment variables (double underscore maps to configuration sections):

| Key | Purpose |
| --- | --- |
| `ConnectionStrings__Default` | PostgreSQL connection |
| `ConnectionStrings__Redis` | Redis cache |
| `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` | Access token signing/validation |
| `RabbitMq__Enabled`, `RabbitMq__Host`, `RabbitMq__Port`, `RabbitMq__User`, `RabbitMq__Password` | Messaging workflow |
| `InternalApi__Key` | Protect internal scraper write endpoints |
| `Brevo__ApiKey`, `Brevo__SenderEmail`, `Brevo__SenderName` | Brevo transactional email; selected when an API key is configured |
| `Smtp__Host`, `Smtp__Port`, `Smtp__SecureSocketOptions`, `Smtp__Username`, `Smtp__Password`, `Smtp__From` | Local SMTP fallback when Brevo is not configured |
| `Frontend__BaseUrl` | Verification/reset links |
| `Cors__AllowedOrigins__0` | Browser origin |
| `Hangfire__DashboardApiKey` | Dashboard access |
| `ProductDiscovery__Tavily__ApiKey` | Optional server-side product candidate discovery; no key means the provider is disabled |
| `ProductDiscovery__Tavily__MonthlyQuota`, `ProductDiscovery__Tavily__MaxResults` | Provider quota and per-search result cap |
| `CurrencyRates__ApiKey` | Optional CurrencyAPI key; absent means public ExchangeRate-API Open Access. Intraday rates require an eligible CurrencyAPI plan. |
| `CurrencyRates__RefreshMinutes` | CurrencyAPI snapshot cache interval (default 1440 minutes to fit daily/free plans; set to the plan's update interval only when eligible). |
| `Accounts__DeletionGraceDays` | Account deletion cancellation window (1–365 days; default 30) |

Account controls include `PATCH /v1/users/me/tracking/pause`, `/resume`, `DELETE /v1/users/me`, and `POST /v1/users/me/deletion/cancel`. Account export is available at `GET /v1/users/me/export?format=json|csv`. Listing exclusions use `PATCH /v1/listings/{id}/exclude|include`. Progressive name search is available at `GET /v1/products/search/stream`; it streams candidate, source failure, `discoveryCompleted`, and terminal events through resumable search sessions. When discovered candidates still need retailer verification, `discoveryCompleted` lets the UI stop presenting source discovery as active while verification continues; the final `completed` event follows after verification.

`POST /v1/currencies/convert` accepts up to 100 `{amount,currencyCode}` pairs and a `targetCurrency`. It returns the original values alongside calculated equivalents and unrounded conversion rates, the provider update time, and attribution URL. Without a key, conversion uses the public ExchangeRate-API Open Access daily USD snapshot. With `CurrencyRates__ApiKey`, it uses CurrencyAPI and caches for `CurrencyRates__RefreshMinutes`; use a refresh interval supported by the configured plan. Both paths reject stale/invalid snapshots and do not guess unsupported currency codes. The UI keeps the original price visible and displays the selected currency equivalent only when a fresh rate is available. Open Access attribution and caching terms are [documented by ExchangeRate-API](https://www.exchangerate-api.com/docs/free); CurrencyAPI's rate interval varies by plan in its [latest rates documentation](https://currencyapi.com/docs/latest/).

Use `docker/.env.example` as a key checklist only. Replace every sample value with generated secrets. Never commit `.env` files or reuse development values in deployment.

## Database and migrations

Migrations are under `Dealio.Infrastructure/Migrations`. The API currently applies pending migrations during startup. This is convenient for local Compose but should be replaced by an explicit, serialized migration step for production. Back up the database before production migrations and verify restoration procedures before rollout. Migrations are additive; do not reset an existing database.

For local development:

```powershell
dotnet ef migrations list --project server/Dealio.Infrastructure --startup-project server/Dealio.API
dotnet ef database update --project server/Dealio.Infrastructure --startup-project server/Dealio.API
```

## Local development and verification

```powershell
dotnet build server/Dealio.slnx
dotnet test server/Dealio.Tests/Dealio.Tests.csproj
npm --prefix client ci
npm --prefix client run build
python -m pip install --requirement scraper/requirements.txt pytest
$env:PYTHONPATH = (Join-Path $PWD 'scraper')
python -m pytest scraper/tests -q
docker compose --env-file docker/.env.example -f docker/docker-compose.yml config --quiet
```

Integration tests use Testcontainers and require Docker. Tests load `appsettings.Testing.json`, use isolated PostgreSQL containers, and do not connect to the Compose database.
