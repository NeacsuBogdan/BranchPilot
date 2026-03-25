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

Implemented in Stage 4:

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
- `Category`
  - groups products and services within a tenant catalog
- `TaxProfile`
  - represents reusable tax treatment configured per tenant
- `CatalogItem`
  - tenant-scoped product or service with a durable code, optional category, and required tax profile
- `LocationPrice`
  - assigns a location-specific price to one catalog item inside the same tenant
- `Promotion`
  - applies a date-ranged percentage discount to one catalog item at one location
- `Customer`
  - tenant-scoped customer identity used by bookings, reminders, and later reporting
- `Booking`
  - tenant-scoped scheduled operational service record linked to one customer and one location
- `BookingLine`
  - snapshot of booked services, durations, and location-specific prices for one booking

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
- category names must be unique within a tenant
- tax profile names must be unique within a tenant
- catalog item codes must be unique within a tenant
- service items require a duration while product items must not define one
- all referenced category, tax profile, and location records must belong to the current tenant
- each catalog item can define at most one price per location
- all location prices for a single item must share the same currency
- promotions can only target locations that already have a defined price for the item
- promotion windows for the same item and location must not overlap
- customer emails must be unique within a tenant
- customers with booking history cannot be deleted
- bookings must contain at least one service line
- only active service catalog items with location pricing can be scheduled as bookings
- all services in one booking must use the same currency at the chosen location
- bookings must be scheduled in the future
- scheduled and confirmed bookings cannot overlap at the same location
- completed or cancelled bookings cannot be rescheduled or cancelled again
