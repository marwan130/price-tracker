# Fly.io Deployment

Deploy the Price Tracker stack on [Fly.io](https://fly.io) as three apps: **API**, **frontend**, and **scraper worker**. PostgreSQL runs on [Fly Postgres](https://fly.io/docs/postgres/).

## Architecture

| Fly app | Config | Image |
|---------|--------|-------|
| `pricetracker-api` | `fly/api.toml` | `docker/Dockerfile` |
| `pricetracker-web` | `fly/frontend.toml` | `docker/Dockerfile.frontend` |
| `pricetracker-scraper` | `fly/scraper.toml` | `docker/Dockerfile.scraper` |

The API reads `DATABASE_URL` from Fly Postgres automatically and maps it to `ConnectionStrings:Default`. When `FLY_APP_NAME` is set, `AllowedHosts` defaults to `{app}.fly.dev` if not configured.

## Prerequisites

- [flyctl](https://fly.io/docs/hands-on/install-flyctl/) installed and logged in
- Gmail SMTP App Password configured locally (see [backend README](../backend/README.md))

## 1. PostgreSQL

```bash
fly postgres create --name pricetracker-db --region ams
fly postgres attach pricetracker-db --app pricetracker-api
```

This sets the `DATABASE_URL` secret on the API app.

## 2. API

```bash
fly apps create pricetracker-api

fly secrets set --app pricetracker-api \
  Jwt__Secret="YOUR_32_CHAR_OR_LONGER_SECRET" \
  InternalApi__Key="YOUR_INTERNAL_API_KEY" \
  Hangfire__DashboardApiKey="YOUR_DASHBOARD_KEY" \
  Frontend__BaseUrl="https://pricetracker-web.fly.dev" \
  Cors__AllowedOrigins__0="https://pricetracker-web.fly.dev" \
  Smtp__Host="smtp.gmail.com" \
  Smtp__Port="587" \
  Smtp__SecureSocketOptions="StartTls" \
  Smtp__Username="you@gmail.com" \
  Smtp__Password="YOUR_GMAIL_APP_PASSWORD" \
  Smtp__From="you@gmail.com" \
  Smtp__FromName="Smart Price Tracker"

fly deploy -c fly/api.toml
```

Verify:

```bash
fly open /health/ready --app pricetracker-api
```

Hangfire dashboard (requires dashboard key):

```text
https://pricetracker-api.fly.dev/hangfire
```

## 3. Frontend

Update `VITE_API_URL` in `fly/frontend.toml` to match your API URL, then:

```bash
fly apps create pricetracker-web
fly deploy -c fly/frontend.toml
```

Or override at deploy time:

```bash
fly deploy -c fly/frontend.toml --build-arg VITE_API_URL=https://pricetracker-api.fly.dev
```

## 4. Scraper worker

```bash
fly apps create pricetracker-scraper

fly secrets set --app pricetracker-scraper \
  Api__BaseUrl="http://pricetracker-api.internal:8080" \
  Api__InternalKey="YOUR_INTERNAL_API_KEY"

fly deploy -c fly/scraper.toml
```

Use the **same** `InternalApi__Key` value as the API. Private networking (`*.internal`) keeps scraper traffic off the public internet.

## Environment reference

| Secret / env | Purpose |
|--------------|---------|
| `DATABASE_URL` | Set automatically by `fly postgres attach` |
| `FLY_APP_NAME` | Set by Fly; used for default `AllowedHosts` |
| `Jwt__Secret` | JWT signing key (32+ characters) |
| `InternalApi__Key` | Scraper → API authentication |
| `Hangfire__DashboardApiKey` | Protects `/hangfire` |
| `Frontend__BaseUrl` | Email verification / password reset links |
| `Cors__AllowedOrigins__0` | Frontend origin for CORS |
| `Smtp__*` | Gmail SMTP (App Password) |
| `VITE_API_URL` | Frontend build arg — public API URL |

## Migrations

The API runs `context.Database.Migrate()` on startup, so the first deploy after Postgres attach applies pending migrations automatically.

## Local Docker (alternative)

For local full-stack testing without Fly, use [docker/.env.example](../../docker/.env.example) and `docker compose up` — see the root README.
