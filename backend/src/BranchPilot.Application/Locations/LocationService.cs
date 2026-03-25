using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Locations;

public sealed class LocationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LocationService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<LocationResponse>> GetLocationsAsync(CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        return await _dbContext.Locations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .Select(location => new LocationResponse(location.Id, location.Name, location.Code, location.TimeZone))
            .ToListAsync(cancellationToken);
    }

    public async Task<LocationResponse> CreateLocationAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureAuthenticatedTenant();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var locationExists = await _dbContext.Locations
            .AnyAsync(location => location.Code == normalizedCode, cancellationToken);

        if (locationExists)
        {
            throw new AppException(409, "Location code already in use", "This location code already exists.");
        }

        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Code = normalizedCode,
            TimeZone = request.TimeZone.Trim(),
            CreatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.Locations.AddAsync(location, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LocationResponse(location.Id, location.Name, location.Code, location.TimeZone);
    }

    private Guid EnsureAuthenticatedTenant()
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.TenantId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }

        return _currentUserContext.TenantId.Value;
    }
}
