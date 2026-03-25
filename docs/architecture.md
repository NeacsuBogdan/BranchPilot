# Architecture

BranchPilot is being built as a modular monolith for the MVP. Stage 4 extends the foundation with customer operations, service bookings, and dashboard visibility without prematurely introducing distributed complexity.

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

Stage 4 application flow:

- `AuthController` handles tenant registration, login, refresh, logout, and current-session lookup
- `LocationService` enforces tenant-scoped location access and creation
- `UserManagementService` owns paged user listing, user creation, membership updates, and last-owner protection
- `CatalogController` exposes catalog reference data, paged catalog items, and create or update commands behind explicit catalog permissions
- `CatalogService` applies tenant boundaries, validates category and tax profile references, enforces pricing consistency, and prevents overlapping promotions per item and location
- `CustomersController` and `CustomerService` provide tenant-scoped customer CRUD with delete protection when booking history exists
- `BookingsController` and `BookingService` create service bookings from catalog pricing, enforce overlap rules, and handle confirm, complete, reschedule, and cancel transitions
- `DashboardController` and `DashboardService` aggregate customer and booking counters plus upcoming workload for the admin dashboard
- `PermissionAuthorizationHandler` translates membership roles into explicit permission checks at the API boundary
- `BranchPilotDbContext` applies query filters to tenant-owned entities such as `Location`, `AppUser`, `Customer`, `Booking`, `BookingLine`, `Membership`, `MembershipLocation`, `RefreshToken`, `Category`, `TaxProfile`, `CatalogItem`, `LocationPrice`, and `Promotion`
- `DemoDataSeeder` creates a realistic demo tenant with two locations, owner/admin users, memberships, location assignments, catalog records, customers, and bookings for local development and portfolio demos

## Frontend

`frontend/web-admin` is the Angular 19 admin workspace using:

- standalone components
- signals
- Angular Material
- ESLint + Prettier
- Playwright

Stage 4 frontend responsibilities:

- public login and organization registration routes
- signal-driven auth state persisted to local storage
- HTTP interceptor with refresh-token retry behavior
- protected admin shell and dashboard
- tenant location list and location creation form
- permission-aware navigation and route guards
- team management page with search, pagination, and access dialogs
- catalog workspace with reference-data forms, item filters, pagination, and create or edit dialogs for pricing and promotions
- customer workspace with search, status filters, and create or edit dialogs
- booking workspace with filters, create dialog, and lifecycle actions for confirm, complete, reschedule, and cancel

## Local infrastructure

Docker Compose provisions:

- PostgreSQL for transactional persistence
- Redis for caching, background processing support, and future distributed coordination needs

## Delivery

GitHub Actions validates:

- backend restore, build, and test
- frontend install, lint, test, and build
