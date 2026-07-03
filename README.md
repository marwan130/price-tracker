# Price Tracker

A full-stack price monitoring platform. Users can search products, track prices across stores, receive price-drop notifications, and inspect historical price trends. The system is split into a secured ASP.NET Core API, a React/Vite frontend, and a standalone scraper worker that records fresh prices through internal API endpoints.

## Project Areas

- [Backend documentation](docs/backend/README.md)
- [Frontend documentation](docs/frontend/README.md)
- [Scraper documentation](docs/scraper/README.md)
- [Database diagrams](docs/erd/)
- [Endpoint diagrams](docs/end-points/)

## Repository Layout

```text
price-tracker/
  backend/     ASP.NET Core API, application services, domain model, EF Core infrastructure, tests
  frontend/    React 19, TypeScript, Vite, Tailwind CSS frontend
  scraper/     .NET worker that fetches active listings, scrapes prices, and posts results
  docker/      Docker and nginx configuration
  docs/        Architecture, API, frontend, backend, scraper, and ERD documentation
  scripts/     Database initialization and migration helpers
```

## Quick Start

Run the API:

```bash
cd backend
dotnet run --project PriceTracker.API
```

Run the frontend:

```bash
cd frontend
npm install
npm run dev
```

Run the scraper:

```bash
cd scraper/PriceTracker.Scraper
dotnet run
```

## Security Notes

- User-facing API actions require JWT authorization by default.
- Auth endpoints are rate limited by client IP.
- Product search is rate limited separately because it performs live external fetches.
- Internal scraper writes require the `X-Internal-Key` header.
- Access tokens expire according to `Jwt:AccessTokenExpiryMinutes`; refresh tokens expire according to `Jwt:RefreshTokenExpiryDays`.
- Production startup validation requires SMTP, CORS origins, specific `AllowedHosts`, and strong secrets — see [backend docs](docs/backend/README.md).

## Production (Docker)

```bash
cd docker
cp .env.example .env   # fill JWT, SMTP, keys, URLs
docker compose up --build
```

Frontend: `http://localhost:3000` · API: `http://localhost:5001` · Health: `http://localhost:5001/health/ready`

## Production (Fly.io)

Deploy to Fly.io with three apps (API, frontend, scraper) plus Fly Postgres. Full guide: [docs/deployment/fly.io.md](docs/deployment/fly.io.md).

```bash
fly postgres create --name pricetracker-db --region ams
fly apps create pricetracker-api
fly postgres attach pricetracker-db --app pricetracker-api
# set secrets (JWT, SMTP, CORS, Frontend__BaseUrl, …) then:
fly deploy -c fly/api.toml
```


Current verification commands:

```bash
dotnet build backend/PriceTracker.slnx
cd frontend && npm run build
```

## License

This project is licensed under the [MIT License](LICENSE).
