using BranchPilot.Api.Endpoints;
using BranchPilot.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace BranchPilot.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapControllers();
        app.MapGet("/", () => TypedResults.Ok(new
        {
            name = "BranchPilot API",
            documentation = "/swagger",
            health = new[] { "/health/live", "/health/ready" }
        })).ExcludeFromDescription();

        app.MapGroup("/api/platform")
            .WithTags("Platform")
            .MapPlatformEndpoints();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponseAsync
        });

        return app;
    }
}
