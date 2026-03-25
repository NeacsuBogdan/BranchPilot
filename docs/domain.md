# Domain Outline

The long-term product direction remains fixed around the following tenant-scoped capabilities:

- Tenant
- Location
- User
- Membership
- Role
- Permission
- EmployeeProfile
- Customer
- CatalogItem
- Category
- TaxProfile
- LocationPrice
- Promotion
- Booking
- BookingLine
- Order
- OrderLine
- AuditLog
- Notification
- RefreshToken
- JobRunHistory

Implemented in Stage 1:

- `Tenant`
  - represents the company boundary for the workspace
- `Location`
  - belongs to a tenant and is the first operational entity exposed in the UI
- `AppUser`
  - tenant-owned identity used for sign-in and seeded demo access
- `RefreshToken`
  - stored per tenant and user to support session renewal and revocation

Current business rules introduced in this stage:

- email addresses must be globally unique across tenants
- tenant slugs are generated from the tenant name and made unique
- location codes are unique within a tenant
- protected location queries are tenant-scoped by default
- a new organization creates exactly one tenant, one owner user, and one primary location
