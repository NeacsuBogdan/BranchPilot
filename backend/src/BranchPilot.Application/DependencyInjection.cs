using BranchPilot.Application.Auth;
using BranchPilot.Application.Bookings;
using BranchPilot.Application.Catalog;
using BranchPilot.Application.Customers;
using BranchPilot.Application.Dashboard;
using BranchPilot.Application.Locations;
using BranchPilot.Application.Users;
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
        services.AddScoped<BookingService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<LocationService>();
        services.AddScoped<UserManagementService>();

        return services;
    }
}
