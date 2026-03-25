# API Examples

Stage 4 exposes authentication, tenant setup, membership-based user administration, tenant-scoped catalog management, customer CRUD, and service booking operations.

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
        "catalog.view",
        "catalog.manage",
        "customers.view",
        "customers.manage",
        "bookings.view",
        "bookings.manage",
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
        "catalog.view",
        "catalog.manage",
        "customers.view",
        "customers.manage",
        "bookings.view",
        "bookings.manage",
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
      "locations.view",
      "customers.view",
      "customers.manage",
      "bookings.view",
      "bookings.manage"
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

## Catalog options

```http
GET /api/catalog/options
Authorization: Bearer <access-token>
Accept: application/json
```

## List catalog items

```http
GET /api/catalog/items?page=1&pageSize=10&search=consult&itemType=Service
Authorization: Bearer <access-token>
Accept: application/json
```

Example response:

```json
{
  "items": [
    {
      "id": "0d3fca24-1841-457b-b7ab-b0db37808a94",
      "name": "Premium Consultation",
      "code": "CONSULT-PREMIUM",
      "itemType": "Service",
      "isActive": true,
      "durationInMinutes": 45,
      "category": {
        "id": "7d7fca24-1841-457b-b7ab-b0db37808a94",
        "name": "Services",
        "description": "Consultation and advisory services."
      },
      "taxProfile": {
        "id": "8d7fca24-1841-457b-b7ab-b0db37808a94",
        "name": "Standard VAT 19%",
        "rate": 19
      },
      "priceRange": {
        "minimumAmount": 220.0,
        "maximumAmount": 260.0,
        "currencyCode": "EUR",
        "locationCount": 2
      },
      "activePromotionCount": 1
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1
}
```

## Create category

```http
POST /api/catalog/categories
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "name": "Membership Plans",
  "description": "Recurring service plans offered by locations."
}
```

## Create catalog item

```http
POST /api/catalog/items
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "name": "Executive Advisory Session",
  "code": "EXEC-ADVISORY",
  "itemType": "Service",
  "categoryId": "7d7fca24-1841-457b-b7ab-b0db37808a94",
  "taxProfileId": "8d7fca24-1841-457b-b7ab-b0db37808a94",
  "description": "Extended advisory session for enterprise customers.",
  "durationInMinutes": 60,
  "isActive": true,
  "locationPrices": [
    {
      "locationId": "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab",
      "priceAmount": 320,
      "currencyCode": "EUR"
    }
  ],
  "promotions": [
    {
      "name": "Launch Week",
      "locationId": "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab",
      "discountPercentage": 12.5,
      "startsAtUtc": "2026-03-26T08:00:00Z",
      "endsAtUtc": "2026-04-02T20:00:00Z"
    }
  ]
}
```

## Dashboard summary

```http
GET /api/dashboard/summary
Authorization: Bearer <access-token>
Accept: application/json
```

## List customers

```http
GET /api/customers?page=1&pageSize=10&search=marin
Authorization: Bearer <access-token>
Accept: application/json
```

## Create customer

```http
POST /api/customers
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "firstName": "Elena",
  "lastName": "Marin",
  "email": "elena.marin@northwind.test",
  "phoneNumber": "+40 721 100 200",
  "notes": "Repeat enterprise customer."
}
```

## Booking options

```http
GET /api/bookings/options
Authorization: Bearer <access-token>
Accept: application/json
```

## Create booking

```http
POST /api/bookings
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "customerId": "9f39b47e-0ef7-4eeb-9e0a-f1c975b7d8ab",
  "locationId": "ba7fe3f1-97ca-4cb3-9b9d-dabf46bf6910",
  "startsAtUtc": "2026-03-30T09:00:00Z",
  "notes": "Premium consultation for next-week branch review.",
  "lines": [
    {
      "catalogItemId": "0d3fca24-1841-457b-b7ab-b0db37808a94",
      "quantity": 1
    }
  ]
}
```

## Confirm booking

```http
POST /api/bookings/0d3fca24-1841-457b-b7ab-b0db37808a94/confirm
Authorization: Bearer <access-token>
Accept: application/json
```

## Reschedule booking

```http
POST /api/bookings/0d3fca24-1841-457b-b7ab-b0db37808a94/reschedule
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "startsAtUtc": "2026-03-30T12:00:00Z",
  "reason": "Customer requested an afternoon slot."
}
```

## Cancel booking

```http
POST /api/bookings/0d3fca24-1841-457b-b7ab-b0db37808a94/cancel
Authorization: Bearer <access-token>
Content-Type: application/json
Accept: application/json

{
  "reason": "Customer is unavailable."
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
