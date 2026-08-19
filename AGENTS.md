# Repository Instructions for Coding Agents

## Project overview (current starter)

This repository is a prepared **.NET 10 Aspire** starter for Smart Parking Navigator.

- Solution: `CarparkAvailability.slnx`
- Projects:
  - `src/CarparkAvailability.ApiApp` — ASP.NET Core API starter surface
  - `src/CarparkAvailability.WebApp` — Blazor Web App starter surface
  - `src/CarparkAvailability.AppHost` — .NET Aspire orchestrator for ApiApp + WebApp
  - `src/CarparkAvailability.ServiceDefaults` — shared service defaults (health, resilience, OpenTelemetry, service discovery)
- Data assets:
  - `data/HDBCarparkInformation.csv`
  - `data/carpark-availability-sample.json`
  - `data/CarparkAvailability.json`
  - `data/carpark-availability.http`
- Documentation: `docs/` (setup, AGENTS/PRD/TRD/app/AI workshop steps, API key guides)

Do not assume application features beyond this scaffold are already implemented.

## Stack and framework facts

- .NET SDK: `10.0.100` requested in `global.json` (10.x installed in dev environments)
- Aspire AppHost SDK: `Aspire.AppHost.Sdk/13.4.6`
- API: ASP.NET Core minimal API + OpenAPI (`ApiApp`)
- Frontend: Blazor (Razor Components with interactive server components) (`WebApp`)
- Shared platform defaults: ASP.NET Core health endpoints, OpenTelemetry, service discovery (`ServiceDefaults`)

## Verified commands

Run from repository root:

```bash
dotnet restore CarparkAvailability.slnx
dotnet build CarparkAvailability.slnx --no-restore
dotnet test CarparkAvailability.slnx --no-build
dotnet run --project src/CarparkAvailability.ApiApp/CarparkAvailability.ApiApp.csproj --no-build
dotnet run --project src/CarparkAvailability.WebApp/CarparkAvailability.WebApp.csproj --no-build
dotnet run --project src/CarparkAvailability.AppHost/CarparkAvailability.AppHost.csproj --no-build
```

Notes:

- `dotnet test` currently succeeds with no test projects in the starter; keep using it once tests are added.
- AppHost run depends on local Aspire/container runtime setup from `docs/00-setup.md`.

## Security and guardrails

- Never commit credentials, API keys, tokens, or secrets.
- Keep secrets in user-secrets on AppHost (`GoogleMaps:ApiKey`, `DataGovSg:ApiKey`), not in source.
- **Do not call data.gov.sg directly from browser code.** Access data.gov.sg only from server-side code (ApiApp).
- Google Maps browser key must remain restricted to allowed origins/APIs.

## Testing, linting, and documentation expectations

- Use existing .NET validation commands (`restore`, `build`, `test`) for every change.
- Do not invent tooling or commands not present in this repository.
- Add/update tests for behavior changes when test projects are introduced.
- Documentation and tests must follow the repository’s eventual `PRD.md` and `TRD.md` once those documents exist.

## Commit and pull request guidance

- Keep commits small and focused; use clear imperative commit messages.
- PRs should include: scope, why, verification commands run, and any docs/test updates.
- Do not include unrelated refactors or generated noise in the same PR.
