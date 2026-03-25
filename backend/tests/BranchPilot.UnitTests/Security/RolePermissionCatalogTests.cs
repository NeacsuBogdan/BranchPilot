using BranchPilot.Application.Security;
using BranchPilot.Domain.Enums;

namespace BranchPilot.UnitTests.Security;

public sealed class RolePermissionCatalogTests
{
    [Fact]
    public void GetPermissions_ForOwner_IncludesUserAndLocationManagement()
    {
        var permissions = RolePermissionCatalog.GetPermissions(MembershipRole.Owner);

        Assert.Contains(PermissionCodes.UsersManage, permissions);
        Assert.Contains(PermissionCodes.LocationsManage, permissions);
    }

    [Fact]
    public void GetPermissions_ForStaff_DoesNotIncludeManagementPermissions()
    {
        var permissions = RolePermissionCatalog.GetPermissions(MembershipRole.Staff);

        Assert.Contains(PermissionCodes.DashboardView, permissions);
        Assert.DoesNotContain(PermissionCodes.UsersManage, permissions);
        Assert.DoesNotContain(PermissionCodes.LocationsManage, permissions);
    }
}
