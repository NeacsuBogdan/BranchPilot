# API Examples

Stage 1 exposes authentication, tenant setup, and tenant-scoped location management.

## Login with seeded demo account

```http
POST /api/auth/login
Content-Type: application/json
Accept: application/json

{
  "email": "owner@branchpilot.demo",
  "password": "BranchPilot!123"
}
```

Example response:

```json
{
  "accessToken": "eyJ...",
  "accessTokenExpiresAtUtc": "2026-03-25T20:00:00+00:00",
  "refreshToken": "hD6...",
  "refreshTokenExpiresAtUtc": "2026-04-08T20:00:00+00:00",
  "session": {
    "user": {
      "id": "2db0f4a0-0d36-4a93-8d2d-6e484638c686",
      "firstName": "Nora",
      "lastName": "West",
      "fullName": "Nora West",
      "email": "owner@branchpilot.demo"
    },
    "tenant": {
      "id": "451bf932-5158-4b18-84d4-0e9d4884153a",
      "name": "Northwind Operations Group",
      "slug": "northwind-operations-group"
    },
    "locations": [
      {
        "id": "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab",
        "name": "Bucharest Central",
        "code": "BUC-CENTRAL",
        "timeZone": "Europe/Bucharest"
      }
    ]
  }
}
```

## Register organization

```http
POST /api/auth/register-organization
Content-Type: application/json
Accept: application/json

{
  "tenantName": "Harbor & Pine Operations",
  "primaryLocationName": "Bucharest Flagship",
  "primaryLocationCode": "BUC-FLAG",
  "primaryLocationTimeZone": "Europe/Bucharest",
  "firstName": "Elena",
  "lastName": "Ionescu",
  "email": "elena@harborpine.test",
  "password": "BranchPilot!123"
}
```

## Current session

```http
GET /api/auth/me
Authorization: Bearer <access-token>
Accept: application/json
```

## Create location

```http
POST /api/locations
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "name": "Timisoara West",
  "code": "TM-WEST",
  "timeZone": "Europe/Bucharest"
}
```

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
