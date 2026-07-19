# Backend README

The backend is an ASP.NET Core API organized as a layered .NET solution.

## Structure

```text
backend/
  PriceTracker.API/             Controllers, middleware, mappings, service registration
  PriceTracker.Application/     DTOs, validators, interfaces, business services
  PriceTracker.Domain/          Entities, enums, domain exceptions
  PriceTracker.Infrastructure/  EF Core persistence, repositories, auth, email, Hangfire jobs
  PriceTracker.Tests/           Unit and integration tests
  PriceTracker.slnx             Solution file
```

## Runtime Responsibilities

- Authenticates users with JWT access tokens and rotating refresh tokens.
- Applies authorization by default; only explicitly marked endpoints are anonymous.
- Protects auth and search endpoints with IP-based rate limiting.
- Serves product, listing, tracking, notification, admin, category, currency, store, scrape log, and price history APIs.
- Uses internal API-key middleware for scraper write endpoints.
- Runs Hangfire recurring jobs for price-alert evaluation.
- Searches products by concurrently running all registered MENA store scrapers (Amazon EG/SA/AE, Noon EG/SA/AE, Jumia EG/SA, Namshi EG/SA/AE, and 34 MENA regional stores via a generic HTML scraper). Results are merged, deduplicated, filtered, sorted, and cached in Redis with a 5-minute TTL.
- Scrapes single product pages by URL on demand, with results cached for 30 minutes.

## Anti-Block Scraping Measures

All scrapers in `PriceTracker.Application/Scrapers/` share `ScraperHelpers` which provides:

- **User-Agent rotation** across 8 real browser UA strings, randomised per request.
- **Exponential backoff** (1s → 2s → 4s, up to 3 attempts) on 429 / 503 responses.
- **Retry-After header** support on 429 / 503 responses.
- **CAPTCHA / block detection** — 12 known block-page patterns matched; affected stores are skipped gracefully.
- **Polite delay** — 400–1200 ms random jitter between page fetches for the same store.

## Configuration

Important settings:

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `ConnectionStrings:Redis` | Redis connection string (optional; disables distributed cache if absent) |
| `Jwt:Secret` | JWT signing secret |
| `Jwt:Issuer` | Expected JWT issuer |
| `Jwt:Audience` | Expected JWT audience |
| `Jwt:AccessTokenExpiryMinutes` | Access token lifetime |
| `Jwt:RefreshTokenExpiryDays` | Refresh token lifetime |
| `InternalApi:Key` | Secret expected in `X-Internal-Key` for scraper calls |
| `Smtp:Host` | SMTP server (Gmail: `smtp.gmail.com`) |
| `Smtp:Port` | SMTP port (Gmail: `587` with StartTLS, or `465` with SSL) |
| `Smtp:SecureSocketOptions` | MailKit option: `StartTls`, `SslOnConnect`, or `None` |
| `Smtp:Username` | SMTP login (Gmail: full `@gmail.com` address) |
| `Smtp:Password` | SMTP password (Gmail: [App Password](https://myaccount.google.com/apppasswords), not your Google account password) |
| `Smtp:From` | Sender address (Gmail: must match the authenticated account or a configured alias) |
| `Smtp:FromName` | Display name shown in the inbox (default: `Smart Price Tracker`) |
| `Frontend:BaseUrl` | Frontend URL used in verification and password-reset email links |
| `Cors:AllowedOrigins` | Allowed frontend origins |
| `Hangfire:DashboardApiKey` | Dashboard access key |

Use environment variables with double underscores, for example `Jwt__Secret` or `Smtp__Password`.

### Gmail SMTP (development / production)

1. Use a dedicated Gmail account (for example `pricetracker33@gmail.com`).
2. Turn on [2-Step Verification](https://myaccount.google.com/security).
3. Create an [App Password](https://myaccount.google.com/apppasswords) for **Mail**.
4. Put the 16-character app password in user secrets (recommended) or `Smtp:Password`:

```bash
dotnet user-secrets set "Smtp:Password" "your-app-password" --project backend/PriceTracker.API
```

Or via environment variable: `Smtp__Password`.
5. Set these keys in `appsettings.Development.json` or env vars:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "SecureSocketOptions": "StartTls",
  "Username": "you@gmail.com",
  "Password": "your-app-password",
  "From": "you@gmail.com",
  "FromName": "Smart Price Tracker"
}
```

Test delivery:

```powershell
./scripts/test-smtp.ps1 you@gmail.com
```

## Development

```bash
cd backend
dotnet restore PriceTracker.slnx
dotnet build PriceTracker.slnx
dotnet run --project PriceTracker.API
```

Swagger is enabled in development at `/swagger`.

## Database

Migrations live in `PriceTracker.Infrastructure/Migrations`.

```bash
cd backend
dotnet ef database update --project PriceTracker.Infrastructure --startup-project PriceTracker.API
```

The scripts folder also includes migration helpers for PowerShell and shell environments.

## Security Expectations

- Keep production `Jwt:Secret`, database credentials, SMTP credentials, and `InternalApi:Key` outside source control.
- Keep `Jwt:AccessTokenExpiryMinutes` short, such as 15 minutes.
- Keep refresh token expiration finite and rotate refresh tokens on every refresh.
- Email verification and password reset tokens are single-use, SHA-256 hashed at rest, and expire via `Auth:EmailVerificationExpiryHours` (default 24) and `Auth:PasswordResetExpiryHours` (default 1).
- Refresh tokens are hashed at rest; password change and reset revoke all active refresh sessions.
- Treat `/v1/price-history` POST and `/v1/scrape-logs` POST as internal scraper endpoints guarded by `X-Internal-Key`.
- In Development the API requires SMTP, CORS origins, and strong secrets to be configured.
- Swagger is enabled in development at `/swagger`.
- Hangfire dashboard requires `Hangfire:DashboardApiKey` via `X-Dashboard-Key` header or `?dashboardKey=` query.
- Health: `GET /health` (liveness), `GET /health/ready` (includes database check).

## Testing

```bash
dotnet build backend/PriceTracker.slnx
dotnet test backend/PriceTracker.Tests/PriceTracker.Tests.csproj
```