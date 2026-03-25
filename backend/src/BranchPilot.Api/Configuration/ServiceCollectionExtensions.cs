using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BranchPilot.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddAuthorization();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BranchPilot API",
                Version = "v1",
                Description = "Bootstrap API surface for the BranchPilot multi-tenant operations platform."
            });
        });

        return services;
    }
}
