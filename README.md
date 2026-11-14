# Dealio

Dealio is a product discovery and automatic price tracking application. The repository contains a rebuilt React frontend, layered ASP.NET Core API, PostgreSQL database, a RabbitMQ-backed targeted scrape workflow, and a Python Scrapling worker.

## Project areas

- [Server and API](docs/SERVER.md)
- [Client](docs/CLIENT.md)
- [Scraping workflow and message contracts](docs/SCRAPER.md)
- [Architecture and contracts](docs/ARCHITECTURE.md)
- [User guide](docs/USER-GUIDE.md)
- [Operations and recovery](docs/OPERATIONS.md)
- [Local quality and security tools](docs/LOCAL-QUALITY-TOOLS.md)
- [Contribution guide](docs/CONTRIBUTING.md)

## Local setup

Copy `docker/.env.example` to `docker/.env`, replace every sample secret, then run:

```powershell
docker compose --env-file docker/.env -f docker/docker-compose.yml up --build
```

The frontend is served on port 3000 and the API on port 5001. PostgreSQL, Redis, and RabbitMQ are private to the Compose network.

For separate development processes:

```powershell
dotnet run --project server/Dealio.API
npm --prefix client run dev
```

The API applies additive EF migrations on startup. Production should use a reviewed migration procedure and a verified backup before schema changes.

## Checks

```powershell
dotnet build server/Dealio.slnx
dotnet test server/Dealio.Tests/Dealio.Tests.csproj
npm --prefix client run build
python -m pytest scraper/tests -q
docker compose --env-file docker/.env.example -f docker/docker-compose.yml config --quiet
```
