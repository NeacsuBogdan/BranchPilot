# BranchPilot

BranchPilot is a portfolio-grade multi-tenant business operations platform for companies with one or more locations.

Stage 1 establishes the product foundation for authentication and tenant setup:

- ASP.NET Core 9 backend split into `Domain`, `Application`, `Infrastructure`, and `Api`
- Angular 19 admin app with standalone components, signals, Angular Material, ESLint, Prettier, and Playwright
- Docker Compose services for PostgreSQL and Redis
- JWT authentication with refresh tokens
- tenant and primary-location registration flow
- seeded demo tenant and protected admin dashboard
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

- Admin UI: `http://localhost:4200/auth/login`
- API platform info: `https://localhost:7247/api/platform/info`
- Live health check: `https://localhost:7247/health/live`
- Readiness health check: `https://localhost:7247/health/ready`
- Swagger UI: `https://localhost:7247/swagger`

The API applies migrations and seeds demo data on startup.

## Demo access

Use the seeded owner account to enter the admin workspace:

- email: `owner@branchpilot.demo`
- password: `BranchPilot!123`

Stage 1 also supports registering a new organization from `/auth/register`. That flow creates:

- one tenant
- one owner account
- one primary location
- one authenticated session with access and refresh tokens

## Stage 1 scope

Implemented in this stage:

- secure organization registration
- seeded demo sign-in
- refresh-token based session renewal
- `/api/auth/me` session endpoint
- tenant-scoped location listing and creation
- protected Angular admin shell and dashboard

Not implemented yet:

- memberships and RBAC policies beyond the tenant owner/admin seed path
- catalog, customers, bookings, reporting, audit, and background jobs

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
npm run e2e
```
