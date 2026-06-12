$ErrorActionPreference = "Stop"

$BackendRoot = Resolve-Path (Join-Path $PSScriptRoot "..\backend")

Push-Location $BackendRoot
try {
    dotnet ef database update `
        --project PriceTracker.Infrastructure `
        --startup-project PriceTracker.API
}
finally {
    Pop-Location
}
