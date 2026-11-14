# Contributing

## Setup

Use the repository root. Copy `docker/.env.example` to `docker/.env` and replace the sample values before starting Compose. For source-only work, install .NET 10, Node 20, Python 3.12, and Docker for PostgreSQL integration tests.

## Checks

```powershell
dotnet build server/Dealio.slnx
dotnet test server/Dealio.Tests/Dealio.Tests.csproj
npm --prefix client ci
npm --prefix client run build
python -m pip install -r scraper/requirements.txt pytest
$env:PYTHONPATH = (Join-Path $PWD 'scraper')
python -m pytest scraper/tests -q
docker compose --env-file docker/.env.example -f docker/docker-compose.yml config --quiet
```

Integration tests use isolated Testcontainers databases. Python scraper fixtures run without image-processing dependencies. Do not run the .NET integration tests against a production database. Do not reset the local or production database to make a test pass.

## Change guidelines

- Preserve existing data through additive migrations; inspect generated SQL and the model snapshot.
- Keep broker payloads versioned and idempotent. Keep domain validation and persistence decisions in the API.
- Add extraction fixtures for every supported store; tests must not require scraping a live store.
- Do not introduce fake price data, hidden fallback providers, paid search APIs, push notifications, or removed research features.
- Update the backend, frontend, deployment config, and docs together when changing a contract.
- Report commands and results exactly; note any unverified external account behavior.
