# Local quality and security tools

Tool binaries, virtual environments, package caches, and scan output stay in ignored directories and are not part of the application build or Git history. Run tools from the repository root with explicit paths.

## Installed tools

| Tool | Version | Local executable |
| --- | --- | --- |
| Graphify | 0.9.101 | `.tools/graphify/venv/Scripts/graphify.exe` |
| Fallow | 3.38.1 | `.tools/fallow/node_modules/.bin/fallow.cmd` |
| CodeQL CLI bundle | 2.27.4 | `.tools/codeql/2.27.4/codeql/codeql.exe` |
| Trivy | 0.76.1 | `.tools/trivy/0.76.1/trivy.exe` |
| OWASP ZAP weekly | D-2026-10-29 | `.tools/zap/D-2026-10-29/ZAP_D-2026-10-29/zap.bat` |

Archives were verified against upstream SHA-256 checksums before installation. This ZAP weekly build requires Java 21, so a portable Temurin JRE 21 is installed under `.tools/java/21.0.13/`; no machine-wide Java or PATH changes were made.

```powershell
& .tools/graphify/venv/Scripts/graphify.exe --version
& .tools/fallow/node_modules/.bin/fallow.cmd --version
& .tools/codeql/2.27.4/codeql/codeql.exe version
& .tools/trivy/0.76.1/trivy.exe --version
$repo = (Get-Location).Path
$env:JAVA_HOME = Join-Path $repo '.tools/java/21.0.13/jdk-21.0.13+9-jre'
$env:Path = (Join-Path $env:JAVA_HOME 'bin') + ';' + $env:Path
$zapHome = Join-Path $repo '.tools/zap-home'
New-Item -ItemType Directory -Force $zapHome | Out-Null
Push-Location '.tools/zap/D-2026-10-29/ZAP_D-2026-10-29'
try { & .\zap.bat -cmd -dir $zapHome -version } finally { Pop-Location }
```

## Running scans

Use `.tools/reports/` for local reports. For a Trivy filesystem scan that excludes secrets and local tool caches:

```powershell
& .tools/trivy/0.76.1/trivy.exe fs --scanners vuln,misconfig --cache-dir .tools/trivy-cache --skip-dirs .git,.tools,.agents,.codex,client/node_modules,server/Dealio.API/bin,server/Dealio.API/obj .
```

CodeQL runs the C# suite against the backend and the JavaScript/TypeScript suite against `client/`; keep SARIF reports and databases under `.tools/reports/`.

Keep active or passive dynamic scanning (ZAP) limited to an isolated local test instance. Do not point it at production or a shared service, and do not run the tool workflows copied from other projects unchanged; they target a different repository layout.

These scans do not replace authenticated API testing, accessibility review, browser journeys, or manual security review. Re-run scans against the current source before treating any past report as release evidence.
