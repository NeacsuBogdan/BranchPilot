using BranchPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<Location> Locations { get; }

    DbSet<AppUser> Users { get; }

    DbSet<Membership> Memberships { get; }

    DbSet<MembershipLocation> MembershipLocations { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
