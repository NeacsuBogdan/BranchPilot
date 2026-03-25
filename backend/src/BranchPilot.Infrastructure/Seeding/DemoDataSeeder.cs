using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Domain.Entities;
using BranchPilot.Domain.Enums;
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
        if (!await _dbContext.Tenants.AnyAsync(cancellationToken))
        {
            await SeedFreshDemoTenantAsync(cancellationToken);
        }

        await EnsureMembershipBackfillAsync(cancellationToken);
        await EnsureCatalogSeedAsync(cancellationToken);
    }

    private async Task SeedFreshDemoTenantAsync(CancellationToken cancellationToken)
    {
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

        var memberships = new[]
        {
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = owner.Id,
                Role = MembershipRole.Owner,
                CreatedAtUtc = now,
            },
            new Membership
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = admin.Id,
                Role = MembershipRole.Admin,
                CreatedAtUtc = now,
            },
        };

        await _dbContext.Tenants.AddAsync(tenant, cancellationToken);
        await _dbContext.Locations.AddRangeAsync(locations, cancellationToken);
        await _dbContext.Users.AddRangeAsync([owner, admin], cancellationToken);
        await _dbContext.Memberships.AddRangeAsync(memberships, cancellationToken);
        await _dbContext.MembershipLocations.AddRangeAsync(
            memberships.SelectMany(
                membership => locations.Select(
                    location => new MembershipLocation
                    {
                        TenantId = tenant.Id,
                        MembershipId = membership.Id,
                        LocationId = location.Id,
                        AssignedAtUtc = now,
                    })),
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded demo tenant {TenantName} with credentials {OwnerEmail} / {Password}.",
            tenant.Name,
            owner.Email,
            "BranchPilot!123");
    }

    private async Task EnsureMembershipBackfillAsync(CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tenants = await _dbContext.Tenants.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var tenant in tenants)
        {
            var locations = await _dbContext.Locations
                .IgnoreQueryFilters()
                .Where(location => location.TenantId == tenant.Id)
                .OrderBy(location => location.CreatedAtUtc)
                .ToListAsync(cancellationToken);
            var users = await _dbContext.Users
                .IgnoreQueryFilters()
                .Where(user => user.TenantId == tenant.Id)
                .OrderBy(user => user.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            if (users.Count == 0 || locations.Count == 0)
            {
                continue;
            }

            var memberships = await _dbContext.Memberships
                .IgnoreQueryFilters()
                .Where(membership => membership.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);
            var membershipLocations = await _dbContext.MembershipLocations
                .IgnoreQueryFilters()
                .Where(assignment => assignment.TenantId == tenant.Id)
                .ToListAsync(cancellationToken);

            var ownerExists = memberships.Any(membership => membership.Role == MembershipRole.Owner);

            foreach (var user in users.Where(user => memberships.All(membership => membership.UserId != user.Id)))
            {
                var role = DetermineRole(user, ownerExists);
                var membership = new Membership
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    UserId = user.Id,
                    Role = role,
                    CreatedAtUtc = now,
                };

                await _dbContext.Memberships.AddAsync(membership, cancellationToken);
                memberships.Add(membership);
                ownerExists = ownerExists || role == MembershipRole.Owner;
            }

            foreach (var membership in memberships.Where(
                         membership => membershipLocations.All(
                             assignment => assignment.MembershipId != membership.Id)))
            {
                foreach (var locationId in GetDefaultLocationIds(membership.Role, locations))
                {
                    var assignment = new MembershipLocation
                    {
                        TenantId = tenant.Id,
                        MembershipId = membership.Id,
                        LocationId = locationId,
                        AssignedAtUtc = now,
                    };

                    await _dbContext.MembershipLocations.AddAsync(assignment, cancellationToken);
                    membershipLocations.Add(assignment);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static MembershipRole DetermineRole(AppUser user, bool ownerExists)
    {
        if (!ownerExists)
        {
            return MembershipRole.Owner;
        }

        return user.NormalizedEmail switch
        {
            "ADMIN@BRANCHPILOT.DEMO" => MembershipRole.Admin,
            _ => MembershipRole.Staff,
        };
    }

    private static IReadOnlyCollection<Guid> GetDefaultLocationIds(
        MembershipRole role,
        IReadOnlyCollection<Location> locations)
    {
        return role switch
        {
            MembershipRole.Owner or MembershipRole.Admin => locations.Select(location => location.Id).ToArray(),
            _ => locations.Take(1).Select(location => location.Id).ToArray(),
        };
    }

    private async Task EnsureCatalogSeedAsync(CancellationToken cancellationToken)
    {
        var demoTenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(tenant => tenant.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (demoTenant is null)
        {
            return;
        }

        var catalogExists = await _dbContext.CatalogItems
            .IgnoreQueryFilters()
            .AnyAsync(item => item.TenantId == demoTenant.Id, cancellationToken);

        if (catalogExists)
        {
            return;
        }

        var locations = await _dbContext.Locations
            .IgnoreQueryFilters()
            .Where(location => location.TenantId == demoTenant.Id)
            .OrderBy(location => location.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (locations.Count == 0)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var categories = new[]
        {
            new Category
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Services",
                Description = "Operational services sold across tenant locations.",
                CreatedAtUtc = now,
            },
            new Category
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Retail",
                Description = "Physical items and add-ons sold alongside bookings and walk-in traffic.",
                CreatedAtUtc = now,
            },
        };

        var taxProfiles = new[]
        {
            new TaxProfile
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Standard VAT 19%",
                Rate = 19m,
                CreatedAtUtc = now,
            },
            new TaxProfile
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                Name = "Reduced VAT 9%",
                Rate = 9m,
                CreatedAtUtc = now,
            },
        };

        var catalogItems = new[]
        {
            new CatalogItem
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                CategoryId = categories[0].Id,
                TaxProfileId = taxProfiles[1].Id,
                Name = "Premium Consultation",
                Code = "CONSULT-PREMIUM",
                ItemType = CatalogItemType.Service,
                Description = "45-minute consultation service used in the demo workspace.",
                DurationInMinutes = 45,
                IsActive = true,
                CreatedAtUtc = now,
            },
            new CatalogItem
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                CategoryId = categories[0].Id,
                TaxProfileId = taxProfiles[1].Id,
                Name = "Express Follow-up",
                Code = "FOLLOWUP-EXPRESS",
                ItemType = CatalogItemType.Service,
                Description = "Short operational follow-up service for repeat customers.",
                DurationInMinutes = 20,
                IsActive = true,
                CreatedAtUtc = now,
            },
            new CatalogItem
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                CategoryId = categories[1].Id,
                TaxProfileId = taxProfiles[0].Id,
                Name = "Retail Care Kit",
                Code = "RETAIL-CARE-KIT",
                ItemType = CatalogItemType.Product,
                Description = "Physical retail bundle sold at selected locations.",
                IsActive = true,
                CreatedAtUtc = now,
            },
        };

        var locationPrices = catalogItems
            .SelectMany(
                item => locations.Select(
                    location => new LocationPrice
                    {
                        CatalogItemId = item.Id,
                        LocationId = location.Id,
                        TenantId = demoTenant.Id,
                        PriceAmount = ResolveSeedPrice(item.Code, location.Code),
                        CurrencyCode = "EUR",
                        UpdatedAtUtc = now,
                    }))
            .ToArray();

        var promotions = new[]
        {
            new Promotion
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                CatalogItemId = catalogItems[0].Id,
                LocationId = locations[0].Id,
                Name = "Spring intro",
                DiscountPercentage = 10m,
                StartsAtUtc = now.AddDays(-5),
                EndsAtUtc = now.AddDays(10),
                CreatedAtUtc = now,
            },
            new Promotion
            {
                Id = Guid.NewGuid(),
                TenantId = demoTenant.Id,
                CatalogItemId = catalogItems[2].Id,
                LocationId = locations[1 % locations.Count].Id,
                Name = "Retail bundle boost",
                DiscountPercentage = 8m,
                StartsAtUtc = now.AddDays(2),
                EndsAtUtc = now.AddDays(20),
                CreatedAtUtc = now,
            },
        };

        await _dbContext.Categories.AddRangeAsync(categories, cancellationToken);
        await _dbContext.TaxProfiles.AddRangeAsync(taxProfiles, cancellationToken);
        await _dbContext.CatalogItems.AddRangeAsync(catalogItems, cancellationToken);
        await _dbContext.LocationPrices.AddRangeAsync(locationPrices, cancellationToken);
        await _dbContext.Promotions.AddRangeAsync(promotions, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static decimal ResolveSeedPrice(string itemCode, string locationCode)
    {
        return (itemCode, locationCode) switch
        {
            ("CONSULT-PREMIUM", "BUC-CENTRAL") => 49m,
            ("CONSULT-PREMIUM", _) => 45m,
            ("FOLLOWUP-EXPRESS", "BUC-CENTRAL") => 28m,
            ("FOLLOWUP-EXPRESS", _) => 25m,
            ("RETAIL-CARE-KIT", "BUC-CENTRAL") => 34m,
            ("RETAIL-CARE-KIT", _) => 31m,
            _ => 20m,
        };
    }
}
