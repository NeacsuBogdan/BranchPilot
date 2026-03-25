using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Domain.Entities;
using BranchPilot.Infrastructure.Auth;
using BranchPilot.Infrastructure.Persistence;
using BranchPilot.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BranchPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddHttpContextAccessor();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddDbContext<BranchPilotDbContext>(options =>
        {
            options.UseNpgsql(postgresConnectionString);
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<BranchPilotDbContext>());

        services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("The API process is accepting requests."),
                tags: ["live"])
            .AddNpgSql(
                postgresConnectionString,
                name: "postgres",
                tags: ["ready"])
            .AddRedis(
                redisConnectionString,
                name: "redis",
                tags: ["ready"]);

        return services;
    }
}
