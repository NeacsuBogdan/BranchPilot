using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Security;
using BranchPilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Infrastructure.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public PermissionAuthorizationHandler(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!_currentUserContext.IsAuthenticated ||
            _currentUserContext.UserId is null ||
            _currentUserContext.TenantId is null)
        {
            return;
        }

        var membershipRole = await (
            from membership in _dbContext.Memberships.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
            where membership.UserId == _currentUserContext.UserId.Value && user.IsActive
            select (MembershipRole?)membership.Role)
            .SingleOrDefaultAsync();

        if (membershipRole is not null &&
            RolePermissionCatalog.GetPermissions(membershipRole.Value).Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
