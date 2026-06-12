#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

cd "$ROOT/backend"

dotnet ef database update \
    --project PriceTracker.Infrastructure \
    --startup-project PriceTracker.API
