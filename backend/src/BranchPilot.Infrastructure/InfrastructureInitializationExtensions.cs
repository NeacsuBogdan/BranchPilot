using BranchPilot.Infrastructure.Persistence;
using BranchPilot.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BranchPilot.Infrastructure;

public static class InfrastructureInitializationExtensions
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);

    public static async Task InitialiseInfrastructureAsync(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        await InitializationLock.WaitAsync();

        try
        {
            using var scope = services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<BranchPilotDbContext>();
            await dbContext.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await seeder.SeedAsync();
        }
        finally
        {
            InitializationLock.Release();
        }
    }
}
