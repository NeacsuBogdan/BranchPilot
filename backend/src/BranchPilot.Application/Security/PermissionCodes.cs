namespace BranchPilot.Application.Security;

public static class PermissionCodes
{
    public const string DashboardView = "dashboard.view";
    public const string LocationsView = "locations.view";
    public const string LocationsManage = "locations.manage";
    public const string CatalogView = "catalog.view";
    public const string CatalogManage = "catalog.manage";
    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";
    public const string BookingsView = "bookings.view";
    public const string BookingsManage = "bookings.manage";
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";

    public static readonly IReadOnlyCollection<string> All =
    [
        DashboardView,
        LocationsView,
        LocationsManage,
        CatalogView,
        CatalogManage,
        CustomersView,
        CustomersManage,
        BookingsView,
        BookingsManage,
        UsersView,
        UsersManage,
    ];
}
