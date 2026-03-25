# Architecture

BranchPilot is being built as a modular monolith for the MVP. Stage 2 extends the foundation with memberships, permission-based authorization, and tenant-safe team management without prematurely introducing distributed complexity.

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
- policy-based authorization backed by membership permissions
- seeded demo tenant initialization on startup
- Serilog console logging
- clean project references aligned to the intended architecture

Stage 2 application flow:

- `AuthController` handles tenant registration, login, refresh, logout, and current-session lookup
- `LocationService` enforces tenant-scoped location access and creation
- `UserManagementService` owns paged user listing, user creation, membership updates, and last-owner protection
- `PermissionAuthorizationHandler` translates membership roles into explicit permission checks at the API boundary
- `BranchPilotDbContext` applies query filters to tenant-owned entities such as `Location`, `AppUser`, `Membership`, `MembershipLocation`, and `RefreshToken`
- `DemoDataSeeder` creates a realistic demo tenant with two locations, owner/admin users, memberships, and location assignments for local development and portfolio demos

## Frontend

`frontend/web-admin` is the Angular 19 admin workspace using:

- standalone components
- signals
- Angular Material
- ESLint + Prettier
- Playwright

Stage 2 frontend responsibilities:

- public login and organization registration routes
- signal-driven auth state persisted to local storage
- HTTP interceptor with refresh-token retry behavior
- protected admin shell and dashboard
- tenant location list and location creation form
- permission-aware navigation and route guards
- team management page with search, pagination, and access dialogs

## Local infrastructure

Docker Compose provisions:

- PostgreSQL for transactional persistence
- Redis for caching, background processing support, and future distributed coordination needs

## Delivery

GitHub Actions validates:

- backend restore, build, and test
- frontend install, lint, test, and build
