# Architecture

BranchPilot is being built as a modular monolith for the MVP. Stage 1 extends the delivery foundation with working authentication, tenant provisioning, and location scoping without prematurely introducing distributed complexity.

## Backend

- `BranchPilot.Domain` contains core domain types and business rules
- `BranchPilot.Application` will host use cases, contracts, and orchestration logic
- `BranchPilot.Infrastructure` owns external integrations and operational concerns such as database, Redis, and jobs
- `BranchPilot.Api` exposes the HTTP surface, Swagger, health checks, and API composition

Current API bootstrap includes:

- Swagger UI for contract discovery
- `/health/live` and `/health/ready` endpoints
- JWT bearer authentication plus refresh tokens
- tenant-safe EF Core query filters for scoped entities
- seeded demo tenant initialization on startup
- Serilog console logging
- clean project references aligned to the intended architecture

Stage 1 application flow:

- `AuthController` handles tenant registration, login, refresh, logout, and current-session lookup
- `LocationService` enforces tenant-scoped location access and creation
- `BranchPilotDbContext` applies query filters to tenant-owned entities such as `Location`, `AppUser`, and `RefreshToken`
- `DemoDataSeeder` creates a realistic demo tenant with two locations and two users for local development and portfolio demos

## Frontend

`frontend/web-admin` is the Angular 19 admin workspace using:

- standalone components
- signals
- Angular Material
- ESLint + Prettier
- Playwright

Stage 1 frontend responsibilities:

- public login and organization registration routes
- signal-driven auth state persisted to local storage
- HTTP interceptor with refresh-token retry behavior
- protected admin shell and dashboard
- tenant location list and location creation form

## Local infrastructure

Docker Compose provisions:

- PostgreSQL for transactional persistence
- Redis for caching, background processing support, and future distributed coordination needs

## Delivery

GitHub Actions validates:

- backend restore, build, and test
- frontend install, lint, test, and build
