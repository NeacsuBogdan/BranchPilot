namespace BranchPilot.Application.Security;

public static class PermissionCodes
{
    public const string DashboardView = "dashboard.view";
    public const string LocationsView = "locations.view";
    public const string LocationsManage = "locations.manage";
    public const string CatalogView = "catalog.view";
    public const string CatalogManage = "catalog.manage";
    public const string UsersView = "users.view";
    public const string UsersManage = "users.manage";

    public static readonly IReadOnlyCollection<string> All =
    [
        DashboardView,
        LocationsView,
        LocationsManage,
        CatalogView,
        CatalogManage,
        UsersView,
        UsersManage,
    ];
}
