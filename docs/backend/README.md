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
- Serves product, listing, tracking, notification, category, currency, store, scrape log, and price history APIs.
- Uses internal API-key middleware for scraper write endpoints.
- Runs Hangfire recurring jobs for price-alert evaluation.

## Configuration

Important settings:

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:Default` | PostgreSQL connection string |
| `Jwt:Secret` | JWT signing secret |
| `Jwt:Issuer` | Expected JWT issuer |
| `Jwt:Audience` | Expected JWT audience |
| `Jwt:AccessTokenExpiryMinutes` | Access token lifetime |
| `Jwt:RefreshTokenExpiryDays` | Refresh token lifetime |
| `InternalApi:Key` | Secret expected in `X-Internal-Key` for scraper calls |
| `Cors:AllowedOrigins` | Allowed frontend origins |
| `Hangfire:DashboardApiKey` | Dashboard access key |

Use environment variables with double underscores, for example `Jwt__Secret`.

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
- Treat `/v1/price-history` POST and `/v1/scrape-logs` POST as internal scraper endpoints guarded by `X-Internal-Key`.

## Verification

```bash
dotnet build backend/PriceTracker.slnx
dotnet test backend/PriceTracker.Tests/PriceTracker.Tests.csproj
```