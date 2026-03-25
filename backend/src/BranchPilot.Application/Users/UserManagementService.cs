using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Application.Security;
using BranchPilot.Domain.Entities;
using BranchPilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Users;

public sealed class UserManagementService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordService _passwordService;

    public UserManagementService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _passwordService = passwordService;
    }

    public Task<IReadOnlyCollection<RoleOptionResponse>> GetRoleOptionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<RoleOptionResponse> roles = Enum.GetValues<MembershipRole>()
            .Select(
                role => new RoleOptionResponse(
                    role.ToString(),
                    role.ToString(),
                    GetRoleDescription(role),
                    RolePermissionCatalog.GetPermissions(role).ToArray()))
            .ToArray();

        return Task.FromResult(roles);
    }

    public async Task<TeamMemberPageResponse> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = EnsureAuthenticatedUser(out _);
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0
            ? Math.Min(request.PageSize, MaxPageSize)
            : DefaultPageSize;
        var normalizedSearch = request.Search?.Trim().ToUpperInvariant();

        var query =
            from membership in _dbContext.Memberships.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
            select new
            {
                User = user,
                Membership = membership,
            };

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(
                item =>
                    item.User.NormalizedEmail.Contains(normalizedSearch) ||
                    item.User.FirstName.ToUpper().Contains(normalizedSearch) ||
                    item.User.LastName.ToUpper().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageItems = await query
            .OrderBy(item => item.User.FirstName)
            .ThenBy(item => item.User.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var locationAssignments = await LoadAssignedLocationsAsync(
            pageItems.Select(item => item.Membership.Id).ToArray(),
            cancellationToken);

        var items = pageItems
            .Select(
                item => ToTeamMemberResponse(
                    item.User,
                    item.Membership,
                    currentUserId,
                    locationAssignments))
            .ToArray();

        return new TeamMemberPageResponse(items, page, pageSize, totalCount);
    }

    public async Task<TeamMemberResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedUser(out _);
        var normalizedEmail = NormalizeEmail(request.Email);
        var role = ParseRole(request.Role);

        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new AppException(409, "Email already in use", "A user with this email address already exists.");
        }

        var assignedLocations = await ResolveAssignedLocationsAsync(request.LocationIds, cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            IsActive = true,
            CreatedAtUtc = now,
        };
        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = user.Id,
            Role = role,
            CreatedAtUtc = now,
        };

        var membershipLocations = assignedLocations
            .Select(
                location => new MembershipLocation
                {
                    TenantId = tenantId,
                    MembershipId = membership.Id,
                    LocationId = location.Id,
                    AssignedAtUtc = now,
                })
            .ToArray();

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.Memberships.AddAsync(membership, cancellationToken);
        await _dbContext.MembershipLocations.AddRangeAsync(membershipLocations, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToTeamMemberResponse(
            user,
            membership,
            _currentUserContext.UserId,
            new Dictionary<Guid, IReadOnlyCollection<LocationAssignmentResponse>>
            {
                [membership.Id] = assignedLocations,
            });
    }

    public async Task<TeamMemberResponse> UpdateUserMembershipAsync(
        Guid userId,
        UpdateUserMembershipRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAuthenticatedUser(out var currentUserId);

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new AppException(404, "User not found", "The selected user could not be found.");
        }

        var membership = await _dbContext.Memberships
            .SingleOrDefaultAsync(existingMembership => existingMembership.UserId == userId, cancellationToken);

        if (membership is null)
        {
            throw new AppException(404, "Membership not found", "The selected user does not have a tenant membership.");
        }

        var nextRole = ParseRole(request.Role);
        await EnsureLastOwnerRemainsAsync(user, membership, nextRole, request.IsActive, cancellationToken);

        var assignedLocations = await ResolveAssignedLocationsAsync(request.LocationIds, cancellationToken);

        user.IsActive = request.IsActive;
        membership.Role = nextRole;

        var existingAssignments = await _dbContext.MembershipLocations
            .Where(assignment => assignment.MembershipId == membership.Id)
            .ToListAsync(cancellationToken);

        _dbContext.MembershipLocations.RemoveRange(existingAssignments);

        var replacementAssignments = assignedLocations
            .Select(
                location => new MembershipLocation
                {
                    TenantId = membership.TenantId,
                    MembershipId = membership.Id,
                    LocationId = location.Id,
                    AssignedAtUtc = _dateTimeProvider.UtcNow,
                })
            .ToArray();

        await _dbContext.MembershipLocations.AddRangeAsync(replacementAssignments, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToTeamMemberResponse(
            user,
            membership,
            currentUserId,
            new Dictionary<Guid, IReadOnlyCollection<LocationAssignmentResponse>>
            {
                [membership.Id] = assignedLocations,
            });
    }

    private Guid EnsureAuthenticatedUser(out Guid? currentUserId)
    {
        if (!_currentUserContext.IsAuthenticated ||
            _currentUserContext.TenantId is null ||
            _currentUserContext.UserId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }

        currentUserId = _currentUserContext.UserId.Value;
        return _currentUserContext.TenantId.Value;
    }

    private async Task<IReadOnlyCollection<LocationAssignmentResponse>> ResolveAssignedLocationsAsync(
        IReadOnlyCollection<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        var distinctLocationIds = locationIds.Distinct().ToArray();

        var assignedLocations = await _dbContext.Locations
            .AsNoTracking()
            .Where(location => distinctLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .Select(location => new LocationAssignmentResponse(location.Id, location.Name, location.Code))
            .ToListAsync(cancellationToken);

        if (assignedLocations.Count != distinctLocationIds.Length)
        {
            throw new AppException(
                400,
                "Assigned location not found",
                "One or more assigned locations could not be found in the current tenant.");
        }

        return assignedLocations;
    }

    private async Task<Dictionary<Guid, IReadOnlyCollection<LocationAssignmentResponse>>> LoadAssignedLocationsAsync(
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken cancellationToken)
    {
        if (membershipIds.Count == 0)
        {
            return [];
        }

        var assignments = await (
            from membershipLocation in _dbContext.MembershipLocations.AsNoTracking()
            join location in _dbContext.Locations.AsNoTracking() on membershipLocation.LocationId equals location.Id
            where membershipIds.Contains(membershipLocation.MembershipId)
            orderby location.Name
            select new
            {
                membershipLocation.MembershipId,
                Location = new LocationAssignmentResponse(location.Id, location.Name, location.Code),
            })
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(assignment => assignment.MembershipId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<LocationAssignmentResponse>)group.Select(item => item.Location).ToArray());
    }

    private async Task EnsureLastOwnerRemainsAsync(
        AppUser user,
        Membership membership,
        MembershipRole nextRole,
        bool nextIsActive,
        CancellationToken cancellationToken)
    {
        if (membership.Role != MembershipRole.Owner || (nextRole == MembershipRole.Owner && nextIsActive))
        {
            return;
        }

        var activeOwnerCount = await (
            from existingMembership in _dbContext.Memberships.AsNoTracking()
            join existingUser in _dbContext.Users.AsNoTracking() on existingMembership.UserId equals existingUser.Id
            where existingMembership.Role == MembershipRole.Owner && existingUser.IsActive
            select existingMembership.Id)
            .CountAsync(cancellationToken);

        if (activeOwnerCount <= 1)
        {
            throw new AppException(
                409,
                "Owner required",
                "At least one active owner must remain assigned to the tenant.");
        }
    }

    private TeamMemberResponse ToTeamMemberResponse(
        AppUser user,
        Membership membership,
        Guid? currentUserId,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<LocationAssignmentResponse>> locationAssignments)
    {
        var assignedLocations = locationAssignments.TryGetValue(membership.Id, out var locations)
            ? locations
            : [];

        return new TeamMemberResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email,
            user.IsActive,
            currentUserId == user.Id,
            new MembershipDetailsResponse(
                membership.Id,
                membership.Role.ToString(),
                RolePermissionCatalog.GetPermissions(membership.Role).ToArray(),
                assignedLocations));
    }

    private static MembershipRole ParseRole(string value)
    {
        if (!MembershipRoleParser.TryParse(value, out var role))
        {
            throw new AppException(400, "Unsupported role", "The selected role is not supported.");
        }

        return role;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string GetRoleDescription(MembershipRole role)
    {
        return role switch
        {
            MembershipRole.Owner => "Full tenant administration including users, locations, and future operational modules.",
            MembershipRole.Admin => "Administrative access for team and location management without ownership transfer semantics.",
            MembershipRole.Manager => "Operational oversight with read access to the team roster and tenant locations.",
            MembershipRole.Staff => "Baseline workspace access for day-to-day work without management privileges.",
            _ => "Standard role.",
        };
    }

}
