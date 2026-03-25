using BranchPilot.Infrastructure.Persistence;
using BranchPilot.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BranchPilot.Infrastructure;

public static class InfrastructureInitializationExtensions
{
    public static async Task InitialiseInfrastructureAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BranchPilotDbContext>();
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }
}
