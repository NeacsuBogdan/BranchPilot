using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Domain.Entities;
using BranchPilot.Domain.Utilities;
using BranchPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BranchPilot.Infrastructure.Seeding;

public sealed class DemoDataSeeder
{
    private readonly BranchPilotDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly IPasswordService _passwordService;

    public DemoDataSeeder(
        BranchPilotDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<DemoDataSeeder> logger,
        IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _passwordService = passwordService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.Tenants.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Northwind Operations Group",
            Slug = TenantSlugGenerator.Generate("Northwind Operations Group"),
            CreatedAtUtc = now,
        };

        var locations = new[]
        {
            new Location
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Bucharest Central",
                Code = "BUC-CENTRAL",
                TimeZone = "Europe/Bucharest",
                CreatedAtUtc = now,
            },
            new Location
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Cluj North",
                Code = "CLJ-NORTH",
                TimeZone = "Europe/Bucharest",
                CreatedAtUtc = now,
            },
        };

        var owner = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FirstName = "Nora",
            LastName = "West",
            Email = "owner@branchpilot.demo",
            NormalizedEmail = "OWNER@BRANCHPILOT.DEMO",
            IsActive = true,
            CreatedAtUtc = now,
        };
        owner.PasswordHash = _passwordService.HashPassword(owner, "BranchPilot!123");

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            FirstName = "Adrian",
            LastName = "Cole",
            Email = "admin@branchpilot.demo",
            NormalizedEmail = "ADMIN@BRANCHPILOT.DEMO",
            IsActive = true,
            CreatedAtUtc = now,
        };
        admin.PasswordHash = _passwordService.HashPassword(admin, "BranchPilot!123");

        await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await _dbContext.Locations.AddRangeAsync(locations, cancellationToken);
        await _dbContext.Users.AddRangeAsync([owner, admin], cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded demo tenant {TenantName} with credentials {OwnerEmail} / {Password}.",
            tenant.Name,
            owner.Email,
            "BranchPilot!123");
    }
}
