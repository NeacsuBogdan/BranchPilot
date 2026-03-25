# Architecture

BranchPilot is being built as a modular monolith for the MVP. The current Stage 0 setup establishes the delivery and runtime foundation without prematurely introducing distributed complexity.

## Backend

- `BranchPilot.Domain` contains core domain types and business rules
- `BranchPilot.Application` will host use cases, contracts, and orchestration logic
- `BranchPilot.Infrastructure` owns external integrations and operational concerns such as database, Redis, and jobs
- `BranchPilot.Api` exposes the HTTP surface, Swagger, health checks, and API composition

Current API bootstrap includes:

- Swagger UI for contract discovery
- `/health/live` and `/health/ready` endpoints
- Serilog console logging
- clean project references aligned to the intended architecture

## Frontend

`frontend/web-admin` is the Angular 19 admin workspace using:

- standalone components
- signals
- Angular Material
- ESLint + Prettier
- Playwright

Stage 0 provides an enterprise admin shell and overview page only. Business workflows begin in later stages.

## Local infrastructure

Docker Compose provisions:

- PostgreSQL for transactional persistence
- Redis for caching, background processing support, and future distributed coordination needs

## Delivery

GitHub Actions validates:

- backend restore, build, and test
- frontend install, lint, test, and build
