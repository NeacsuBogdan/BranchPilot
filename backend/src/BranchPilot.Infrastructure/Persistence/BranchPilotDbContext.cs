using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Infrastructure.Persistence;

public sealed class BranchPilotDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserContext _currentUserContext;

    public BranchPilotDbContext(
        DbContextOptions<BranchPilotDbContext> options,
        ICurrentUserContext currentUserContext)
        : base(options)
    {
        _currentUserContext = currentUserContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BranchPilotDbContext).Assembly);

        modelBuilder.Entity<Location>()
            .HasQueryFilter(location =>
                _currentUserContext.TenantId.HasValue &&
                location.TenantId == _currentUserContext.TenantId.Value);

        modelBuilder.Entity<AppUser>()
            .HasQueryFilter(user =>
                _currentUserContext.TenantId.HasValue &&
                user.TenantId == _currentUserContext.TenantId.Value);

        modelBuilder.Entity<RefreshToken>()
            .HasQueryFilter(token =>
                _currentUserContext.TenantId.HasValue &&
                token.TenantId == _currentUserContext.TenantId.Value);
    }
}
