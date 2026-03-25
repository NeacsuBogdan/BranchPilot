using BranchPilot.Application.Abstractions.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BranchPilot.Infrastructure.Persistence;

public sealed class DesignTimeBranchPilotDbContextFactory : IDesignTimeDbContextFactory<BranchPilotDbContext>
{
    public BranchPilotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BranchPilotDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=branchpilot;Username=branchpilot;Password=branchpilot");

        return new BranchPilotDbContext(optionsBuilder.Options, new DesignTimeCurrentUserContext());
    }

    private sealed class DesignTimeCurrentUserContext : ICurrentUserContext
    {
        public bool IsAuthenticated => false;

        public Guid? UserId => null;

        public Guid? TenantId => null;

        public string? Email => null;
    }
}
