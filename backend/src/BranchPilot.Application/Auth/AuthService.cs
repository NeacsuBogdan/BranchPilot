using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Application.Locations;
using BranchPilot.Application.Security;
using BranchPilot.Domain.Entities;
using BranchPilot.Domain.Enums;
using BranchPilot.Domain.Utilities;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Auth;

public sealed class AuthService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IPasswordService passwordService,
        ITokenService tokenService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<AuthenticatedSessionResponse> RegisterOrganizationAsync(
        RegisterOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new AppException(409, "Email already in use", "A user with this email address already exists.");
        }

        var now = _dateTimeProvider.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.TenantName.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.TenantName, cancellationToken),
            CreatedAtUtc = now,
        };

        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = request.PrimaryLocationName.Trim(),
            Code = NormalizeLocationCode(request.PrimaryLocationCode),
            TimeZone = request.PrimaryLocationTimeZone.Trim(),
            CreatedAtUtc = now,
        };

        var locationCodeExists = await _dbContext.Locations
            .IgnoreQueryFilters()
            .AnyAsync(
                existingLocation => existingLocation.TenantId == tenant.Id && existingLocation.Code == location.Code,
                cancellationToken);

        if (locationCodeExists)
        {
            throw new AppException(409, "Location code already in use", "The primary location code is already in use.");
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            IsActive = true,
            CreatedAtUtc = now,
        };
        user.PasswordHash = _passwordService.HashPassword(user, request.Password);

        await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await _dbContext.Locations.AddAsync(location, cancellationToken);
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.Memberships.AddAsync(
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                Role = MembershipRole.Owner,
                CreatedAtUtc = now,
            },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var membership = await _dbContext.Memberships
            .IgnoreQueryFilters()
            .SingleAsync(existingMembership => existingMembership.UserId == user.Id, cancellationToken);

        await _dbContext.MembershipLocations.AddAsync(
            new MembershipLocation
            {
                TenantId = tenant.Id,
                MembershipId = membership.Id,
                LocationId = location.Id,
                AssignedAtUtc = now,
            },
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var tokenPair = _tokenService.CreateTokenPair(user, tenant, now);
        await PersistRefreshTokenAsync(user, tenant, tokenPair, now, cancellationToken);

        var session = await BuildSessionAsync(user, tenant, cancellationToken, ignoreTenantFilters: true);

        return new AuthenticatedSessionResponse(
            tokenPair.AccessToken,
            tokenPair.AccessTokenExpiresAtUtc,
            tokenPair.RefreshToken,
            tokenPair.RefreshTokenExpiresAtUtc,
            session);
    }

    public async Task<AuthenticatedSessionResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(existingUser => existingUser.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !_passwordService.VerifyPassword(user, request.Password))
        {
            throw new AppException(401, "Invalid credentials", "The email or password is incorrect.");
        }

        var tenant = await _dbContext.Tenants
            .SingleAsync(existingTenant => existingTenant.Id == user.TenantId, cancellationToken);

        var now = _dateTimeProvider.UtcNow;
        var tokenPair = _tokenService.CreateTokenPair(user, tenant, now);
        await PersistRefreshTokenAsync(user, tenant, tokenPair, now, cancellationToken);

        var session = await BuildSessionAsync(user, tenant, cancellationToken, ignoreTenantFilters: true);

        return new AuthenticatedSessionResponse(
            tokenPair.AccessToken,
            tokenPair.AccessTokenExpiresAtUtc,
            tokenPair.RefreshToken,
            tokenPair.RefreshTokenExpiresAtUtc,
            session);
    }

    public async Task<AuthenticatedSessionResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var existingRefreshToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        var now = _dateTimeProvider.UtcNow;

        if (existingRefreshToken is null ||
            existingRefreshToken.RevokedAtUtc is not null ||
            existingRefreshToken.ExpiresAtUtc <= now)
        {
            throw new AppException(401, "Invalid refresh token", "The refresh token is invalid or expired.");
        }

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .SingleAsync(existingUser => existingUser.Id == existingRefreshToken.UserId, cancellationToken);

        if (!user.IsActive)
        {
            throw new AppException(401, "User inactive", "The user account is inactive.");
        }

        var tenant = await _dbContext.Tenants
            .SingleAsync(existingTenant => existingTenant.Id == existingRefreshToken.TenantId, cancellationToken);

        existingRefreshToken.RevokedAtUtc = now;

        var tokenPair = _tokenService.CreateTokenPair(user, tenant, now);
        await PersistRefreshTokenAsync(user, tenant, tokenPair, now, cancellationToken);

        var session = await BuildSessionAsync(user, tenant, cancellationToken, ignoreTenantFilters: true);

        return new AuthenticatedSessionResponse(
            tokenPair.AccessToken,
            tokenPair.AccessTokenExpiresAtUtc,
            tokenPair.RefreshToken,
            tokenPair.RefreshTokenExpiresAtUtc,
            session);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);

        var existingRefreshToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existingRefreshToken is null)
        {
            return;
        }

        existingRefreshToken.RevokedAtUtc ??= _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentSessionResponse> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserContext.IsAuthenticated ||
            _currentUserContext.UserId is null ||
            _currentUserContext.TenantId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existingUser => existingUser.Id == _currentUserContext.UserId.Value,
                cancellationToken);

        if (user is null)
        {
            throw new AppException(404, "User not found", "The signed-in user could not be found.");
        }

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .SingleAsync(existingTenant => existingTenant.Id == _currentUserContext.TenantId.Value, cancellationToken);

        return await BuildSessionAsync(user, tenant, cancellationToken, ignoreTenantFilters: false);
    }

    private async Task PersistRefreshTokenAsync(
        AppUser user,
        Tenant tenant,
        TokenPair tokenPair,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        await _dbContext.RefreshTokens.AddAsync(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                TokenHash = _tokenService.HashRefreshToken(tokenPair.RefreshToken),
                ExpiresAtUtc = tokenPair.RefreshTokenExpiresAtUtc,
                CreatedAtUtc = createdAtUtc,
            },
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CurrentSessionResponse> BuildSessionAsync(
        AppUser user,
        Tenant tenant,
        CancellationToken cancellationToken,
        bool ignoreTenantFilters)
    {
        IQueryable<Location> locationsQuery = _dbContext.Locations.AsNoTracking();
        IQueryable<Membership> membershipsQuery = _dbContext.Memberships.AsNoTracking();
        IQueryable<MembershipLocation> membershipLocationsQuery = _dbContext.MembershipLocations.AsNoTracking();

        if (ignoreTenantFilters)
        {
            locationsQuery = locationsQuery.IgnoreQueryFilters();
            membershipsQuery = membershipsQuery.IgnoreQueryFilters();
            membershipLocationsQuery = membershipLocationsQuery.IgnoreQueryFilters();
        }

        var locations = await locationsQuery
            .Where(location => location.TenantId == tenant.Id)
            .OrderBy(location => location.Name)
            .Select(location => new LocationResponse(location.Id, location.Name, location.Code, location.TimeZone))
            .ToListAsync(cancellationToken);

        var membership = await membershipsQuery
            .SingleOrDefaultAsync(
                existingMembership => existingMembership.TenantId == tenant.Id && existingMembership.UserId == user.Id,
                cancellationToken);

        if (membership is null)
        {
            throw new AppException(
                403,
                "Membership required",
                "The signed-in user does not have an active membership for this tenant.");
        }

        var assignedLocationIds = await membershipLocationsQuery
            .Where(assignment => assignment.MembershipId == membership.Id)
            .Select(assignment => assignment.LocationId)
            .ToListAsync(cancellationToken);

        var assignedLocations = locations
            .Where(location => assignedLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .ToList();

        return new CurrentSessionResponse(
            new UserSummaryResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                $"{user.FirstName} {user.LastName}".Trim(),
                user.Email),
            new MembershipSummaryResponse(
                membership.Id,
                membership.Role.ToString(),
                RolePermissionCatalog.GetPermissions(membership.Role).ToArray(),
                assignedLocations),
            new TenantSummaryResponse(tenant.Id, tenant.Name, tenant.Slug),
            locations);
    }

    private async Task<string> GenerateUniqueSlugAsync(string tenantName, CancellationToken cancellationToken)
    {
        var baseSlug = TenantSlugGenerator.Generate(tenantName);
        var candidate = baseSlug;
        var suffix = 2;

        while (await _dbContext.Tenants.AnyAsync(tenant => tenant.Slug == candidate, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string NormalizeLocationCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }
}
