# Price Tracker

Price Tracker is a platform that enables users to monitor product prices across multiple online stores within specific regions, receive notifications when prices drop below a target value, and analyze historical pricing trends over time. The system continuously collects and stores product pricing data from different stores through scheduled background scraping jobs, enabling users to compare prices, track fluctuations, and make informed purchasing decisions.

## Project Structure

```
price-tracker/
├── backend/                         
│   ├── PriceTracker.API/            # Controllers, Middleware, Mappings, Extensions
│   ├── PriceTracker.Application/    # DTOs, Interfaces, Services, Validators
│   ├── PriceTracker.Domain/         # Entities, Enums, Exceptions
│   ├── PriceTracker.Infrastructure/ # EF Core, Authentication, Email, Jobs
│   ├── PriceTracker.Tests/          # Unit & Integration tests
│   └── PriceTracker.slnx            # Solution file
├── frontend/                        # Frontend 
├── scraper/                         # Standalone price scraper worker
│   └── PriceTracker.Scraper/        # Fetches listings, scrapes pages, posts price history
├── docker/                          # Dockerfile & docker-compose
├── docs/                            # ERD diagrams & endpoint documentation
│   ├── erd/                         # Database ERD diagrams
│   └── end-points/                  # API endpoint specifications
└── scripts/                         # Database init & seed scripts
```

## Tech Stack

- **Backend:** .NET 10, ASP.NET Core, Entity Framework Core
- **Database:** PostgreSQL
- **Authentication:** JWT Bearer tokens
- **Validation:** 
- **Mapping:** 
- **Email:** 
- **Job Scheduling:** 
- **Testing:** 

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL

### Local development

```bash
cd backend
dotnet run --project PriceTracker.API
```

1. Copy `PriceTracker.API/appsettings.Development.json` locally with your database and dev secrets.
2. Set `ASPNETCORE_ENVIRONMENT=Development` (default in `launchSettings.json`).
3. Apply migrations (see [Database migrations](#database-migrations)).

Swagger: `http://localhost:5000/swagger`

### Scraper worker

```bash
cd scraper/PriceTracker.Scraper
dotnet run
```

Set `Api__BaseUrl` and `Api__InternalKey` to match the API. See `scraper/README.md`.

### Database migrations

**Strategy:** run `dotnet ef database update` as a separate deploy step (not at API startup).

Migrations live in `PriceTracker.Infrastructure/Migrations`. The initial migration enables the PostgreSQL `pg_trgm` extension (`CREATE EXTENSION IF NOT EXISTS pg_trgm`).

**Local / manual:**

```bash
# from repo root
./scripts/migrate-database.sh

# or on Windows
./scripts/migrate-database.ps1

# or directly from backend/
cd backend
dotnet ef database update --project PriceTracker.Infrastructure --startup-project PriceTracker.API
```

**Deploy / CI:** run the same command after the database is reachable and before starting the API. `ConnectionStrings__Default` must point at the target database. The command is idempotent — only pending migrations are applied.

**Check status:**

```bash
cd backend
dotnet ef migrations list --project PriceTracker.Infrastructure --startup-project PriceTracker.API
```

Applied migrations show without a `(Pending)` suffix.

### Configuration

`appsettings.json` defines structure and non-secret defaults. 

| File | Purpose |
|------|---------|
| `appsettings.json` | Base config (committed, no secrets) |
| `appsettings.Development.json` | Local overrides (gitignored) |
| `appsettings.Production.json` | Production overrides (gitignored) |

ASP.NET Core merges environment-specific files when `ASPNETCORE_ENVIRONMENT` is set.

In **Production**, the API fails on startup if required settings are missing. Admin seeding runs only in **Development**.

### Environment variables

Use double underscores for nested keys (override any appsettings value):

| Variable | Required (Production) | Description |
|----------|----------------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Yes | `Development`, `Staging`, or `Production` |
| `ConnectionStrings__Default` | Yes | PostgreSQL connection string |
| `Jwt__Secret` | Yes | Signing key for access tokens |
| `InternalApi__Key` | Yes | `X-Internal-Key` header for internal scrape/price-history calls |
| `Jwt__Issuer` | No | Default: `SmartPriceTracker` |
| `Jwt__Audience` | No | Default: `SmartPriceTrackerUsers` |
| `Jwt__AccessTokenExpiryMinutes` | No | Default: `15` |
| `Jwt__RefreshTokenExpiryDays` | No | Default: `7` |
| `Smtp__Host` | No* | SMTP host for alert emails |
| `Smtp__Port` | No* | SMTP port (default `587`) |
| `Smtp__Username` | No* | SMTP username |
| `Smtp__Password` | No* | SMTP password |
| `Smtp__From` | No* | Sender address |
| `Seed__Admin__Email` | No | Dev only — ignored outside Development |
| `Seed__Admin__Password` | No | Dev only — ignored outside Development |
| `AllowedHosts` | Yes (Production) | Comma-separated hostnames — must not be `*` |
| `Hangfire__DashboardApiKey` | Yes | `X-Dashboard-Key` header or `?dashboardKey=` query for `/hangfire` |
| `Cors__AllowedOrigins__0` | When using frontend | Allowed CORS origin (repeat index for multiple origins) |

\*Required once email notifications are enabled.

### Production setup

1. Use `appsettings.json` as the key reference (structure is committed, secrets stay empty).
2. Provide values via `appsettings.Production.json` (gitignored) and/or environment variables / secret store.
3. Set `ASPNETCORE_ENVIRONMENT=Production`.
4. Run `./scripts/migrate-database.ps1` (or the CI equivalent) before starting the API.
5. Do **not** set `Seed:Admin` in production — admin users should be created deliberately.