# API Examples

Stage 0 exposes bootstrap and diagnostics endpoints only.

## Platform info

```http
GET /api/platform/info
Accept: application/json
```

Example response:

```json
{
  "name": "BranchPilot API",
  "environment": "Development",
  "documentation": "/swagger",
  "healthEndpoints": [
    "/health/live",
    "/health/ready"
  ]
}
```

## Live health

```http
GET /health/live
Accept: application/json
```

## Readiness health

```http
GET /health/ready
Accept: application/json
```

The readiness endpoint is expected to fail until PostgreSQL and Redis are running locally.
