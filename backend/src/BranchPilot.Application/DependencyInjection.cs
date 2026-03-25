using BranchPilot.Application.Auth;
using BranchPilot.Application.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BranchPilot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthService>();
        services.AddScoped<LocationService>();

        return services;
    }
}
