using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Security;

public static class RolePermissionCatalog
{
    private static readonly IReadOnlyDictionary<MembershipRole, IReadOnlyCollection<string>> PermissionsByRole =
        new Dictionary<MembershipRole, IReadOnlyCollection<string>>
        {
            [MembershipRole.Owner] =
            [
                PermissionCodes.DashboardView,
                PermissionCodes.LocationsView,
                PermissionCodes.LocationsManage,
                PermissionCodes.CatalogView,
                PermissionCodes.CatalogManage,
                PermissionCodes.CustomersView,
                PermissionCodes.CustomersManage,
                PermissionCodes.BookingsView,
                PermissionCodes.BookingsManage,
                PermissionCodes.UsersView,
                PermissionCodes.UsersManage,
            ],
            [MembershipRole.Admin] =
            [
                PermissionCodes.DashboardView,
                PermissionCodes.LocationsView,
                PermissionCodes.LocationsManage,
                PermissionCodes.CatalogView,
                PermissionCodes.CatalogManage,
                PermissionCodes.CustomersView,
                PermissionCodes.CustomersManage,
                PermissionCodes.BookingsView,
                PermissionCodes.BookingsManage,
                PermissionCodes.UsersView,
                PermissionCodes.UsersManage,
            ],
            [MembershipRole.Manager] =
            [
                PermissionCodes.DashboardView,
                PermissionCodes.LocationsView,
                PermissionCodes.CatalogView,
                PermissionCodes.CatalogManage,
                PermissionCodes.CustomersView,
                PermissionCodes.CustomersManage,
                PermissionCodes.BookingsView,
                PermissionCodes.BookingsManage,
                PermissionCodes.UsersView,
            ],
            [MembershipRole.Staff] =
            [
                PermissionCodes.DashboardView,
                PermissionCodes.LocationsView,
                PermissionCodes.CatalogView,
                PermissionCodes.CustomersView,
                PermissionCodes.CustomersManage,
                PermissionCodes.BookingsView,
                PermissionCodes.BookingsManage,
            ],
        };

    public static IReadOnlyCollection<string> GetPermissions(MembershipRole role)
    {
        return PermissionsByRole[role];
    }
}
