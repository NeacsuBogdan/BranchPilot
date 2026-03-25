# API Examples

Stage 2 exposes authentication, tenant setup, tenant-scoped location management, and membership-based user administration.

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
    "membership": {
      "id": "5813f64a-f4a8-4e5d-bd2d-f9199b6df254",
      "role": "Owner",
      "permissions": [
        "dashboard.view",
        "locations.view",
        "locations.manage",
        "users.view",
        "users.manage"
      ],
      "assignedLocations": [
        {
          "id": "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab",
          "name": "Bucharest Central",
          "code": "BUC-CENTRAL",
          "timeZone": "Europe/Bucharest"
        },
        {
          "id": "ba7fe3f1-97ca-4cb3-9b9d-dabf46bf6910",
          "name": "Cluj North",
          "code": "CLJ-NORTH",
          "timeZone": "Europe/Bucharest"
        }
      ]
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
      },
      {
        "id": "ba7fe3f1-97ca-4cb3-9b9d-dabf46bf6910",
        "name": "Cluj North",
        "code": "CLJ-NORTH",
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

## List available role options

```http
GET /api/users/roles
Authorization: Bearer <access-token>
Accept: application/json
```

Example response:

```json
[
  {
    "code": "Owner",
    "name": "Owner",
    "description": "Full tenant administration including protected access changes.",
    "permissions": [
      "dashboard.view",
      "locations.view",
      "locations.manage",
      "users.view",
      "users.manage"
    ]
  },
  {
    "code": "Staff",
    "name": "Staff",
    "description": "Workspace access for day-to-day operations without administrative privileges.",
    "permissions": [
      "dashboard.view",
      "locations.view"
    ]
  }
]
```

## List tenant users

```http
GET /api/users?page=1&pageSize=10&search=owner
Authorization: Bearer <access-token>
Accept: application/json
```

## Create a tenant user

```http
POST /api/users
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "firstName": "Bianca",
  "lastName": "Matei",
  "email": "bianca.matei@northwind.test",
  "password": "BranchPilot!123",
  "role": "Manager",
  "locationIds": [
    "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab"
  ]
}
```

## Update role and location assignments

```http
PUT /api/users/2db0f4a0-0d36-4a93-8d2d-6e484638c686/membership
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "role": "Admin",
  "isActive": true,
  "locationIds": [
    "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab",
    "ba7fe3f1-97ca-4cb3-9b9d-dabf46bf6910"
  ]
}
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
