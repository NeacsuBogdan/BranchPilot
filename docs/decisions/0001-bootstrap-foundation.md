# ADR 0001: Bootstrap Foundation

## Status

Accepted

## Context

BranchPilot needs to look and behave like a real enterprise product while still progressing in small, reviewable stages. The MVP does not need distributed services, but it does need clean boundaries, realistic infrastructure, and a professional delivery story.

## Decision

- Use a modular monolith backend with clean project boundaries
- Use Angular 19 for the admin experience
- Provision PostgreSQL and Redis through Docker Compose for local development
- Keep `main` stable and perform active work on `dev`
- Add CI early so later stages inherit a working validation pipeline

## Consequences

- The repository is simple enough for staged delivery
- Core enterprise concerns are visible from the start
- Future stages can add tenancy, auth, background jobs, and reporting without restructuring the repo
