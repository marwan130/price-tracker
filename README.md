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
  .github/     GitHub Actions CI/CD workflow
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

Or run everything at once with Docker Compose:

```bash
cp docker/.env.example docker/.env   
docker compose -f docker/docker-compose.yml up --build
```

## Security Notes

- User-facing API actions require JWT authorization by default.
- Auth endpoints are rate limited by client IP.
- Product search is rate limited separately because it performs live external fetches.
- Internal scraper writes require the `X-Internal-Key` header.
- Access tokens expire according to `Jwt:AccessTokenExpiryMinutes`; refresh tokens expire according to `Jwt:RefreshTokenExpiryDays`.
- Development startup validation requires SMTP, CORS origins, and strong secrets — see [backend docs](docs/backend/README.md).

## CI/CD and Deployment

Every push to `main` triggers the GitHub Actions workflow at `.github/workflows/azure-deploy.yml`:

1. **Build and push** — Docker images for the backend, frontend, and scraper are built and pushed to GitHub Container Registry (GHCR).
2. **Deploy** — All three images are deployed to Azure Container Apps via the Azure CLI.
3. **Health check** — After deployment the pipeline hits `GET /health` on the live backend and fails the workflow if the service does not respond, so broken deployments are caught automatically.

### Azure infrastructure

| Resource | Name |
| --- | --- |
| Resource group | `price-tracker-rg` |
| Container Apps environment | `price-tracker-env` |
| Backend container app | `price-tracker-api` |
| Frontend container app | `price-tracker-web` |
| Scraper container app | `price-tracker-scraper` |

### Required GitHub secrets

| Secret | Purpose |
| --- | --- |
| `AZURE_CREDENTIALS` | Azure service principal JSON |
| `GHCR_PAT` | Personal access token for GHCR (falls back to `GITHUB_TOKEN`) |
| `DB_CONNECTION_STRING` | PostgreSQL connection string |
| `JWT_SECRET` | JWT signing secret |
| `INTERNAL_API_KEY` | Scraper internal API key |
| `HANGFIRE_KEY` | Hangfire dashboard API key |
| `SMTP_USERNAME` | SMTP username |
| `SMTP_PASSWORD` | SMTP app password |
| `SMTP_FROM` | SMTP sender address |
| `API_URL` | Public backend URL |

## Development

```bash
dotnet build backend/PriceTracker.slnx
cd frontend && npm run build
```

## License

This project is licensed under the [MIT License](LICENSE).
