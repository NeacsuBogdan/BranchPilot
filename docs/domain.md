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

Implemented in Stage 2:

- `Tenant`
  - represents the company boundary for the workspace
- `Location`
  - belongs to a tenant and is the first operational entity exposed in the UI
- `AppUser`
  - tenant-owned identity used for sign-in and seeded demo access
- `Membership`
  - assigns one tenant-scoped operational role to a user
- `MembershipLocation`
  - constrains a membership to one or more tenant locations
- `RefreshToken`
  - stored per tenant and user to support session renewal and revocation
- `Permission`
  - modeled as explicit application-level codes mapped from extendable roles

Current business rules introduced in this stage:

- email addresses must be globally unique across tenants
- tenant slugs are generated from the tenant name and made unique
- location codes are unique within a tenant
- protected location queries are tenant-scoped by default
- a new organization creates exactly one tenant, one owner user, one owner membership, and one primary location
- each tenant user can have exactly one membership record
- assigned membership locations must belong to the same tenant as the membership
- the last active owner in a tenant cannot be demoted or deactivated
- backend authorization is performed against explicit permission policies instead of implicit UI assumptions
