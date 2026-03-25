# BranchPilot

BranchPilot is a portfolio-grade multi-tenant business operations platform for companies with one or more locations.

Stage 0 establishes the professional bootstrap for the product:

- ASP.NET Core 9 backend split into `Domain`, `Application`, `Infrastructure`, and `Api`
- Angular 19 admin app with standalone components, signals, Angular Material, ESLint, Prettier, and Playwright
- Docker Compose services for PostgreSQL and Redis
- Swagger, health checks, Serilog, solution wiring, and base CI

## Branching model

- `main` remains the stable branch
- `dev` is the active development branch
- all staged implementation work happens on `dev`

## Repository structure

```text
backend/
  src/
    BranchPilot.Domain/
    BranchPilot.Application/
    BranchPilot.Infrastructure/
    BranchPilot.Api/
  tests/
    BranchPilot.UnitTests/
    BranchPilot.IntegrationTests/
frontend/
  web-admin/
docs/
  architecture.md
  domain.md
  api-examples.md
  decisions/
```

## Prerequisites

- .NET 9 SDK
- Node.js 22+
- Docker Desktop

## Local setup

Start the local infrastructure:

```powershell
docker compose up -d
```

Run the backend API:

```powershell
dotnet restore BranchPilot.sln
dotnet run --project backend/src/BranchPilot.Api
```

Run the Angular admin app:

```powershell
cd frontend/web-admin
npm install
npm start
```

Useful local endpoints:

- API platform info: `https://localhost:7247/api/platform/info`
- Live health check: `https://localhost:7247/health/live`
- Readiness health check: `https://localhost:7247/health/ready`
- Swagger UI: `https://localhost:7247/swagger`

## Verification

Backend:

```powershell
dotnet build BranchPilot.sln
dotnet test BranchPilot.sln
```

Frontend:

```powershell
cd frontend/web-admin
npm run lint
npm run test:ci
npm run build
```
