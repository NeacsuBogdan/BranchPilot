namespace BranchPilot.Api.Endpoints;

public static class PlatformEndpoints
{
    public static RouteGroupBuilder MapPlatformEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapGet("/info", (IWebHostEnvironment environment) =>
            TypedResults.Ok(new PlatformInfoResponse(
                "BranchPilot API",
                environment.EnvironmentName,
                "/swagger",
                ["/health/live", "/health/ready"])))
            .WithName("GetPlatformInfo")
            .WithSummary("Returns bootstrap metadata for the API.");

        return group;
    }
}

public sealed record PlatformInfoResponse(
    string Name,
    string Environment,
    string Documentation,
    IReadOnlyCollection<string> HealthEndpoints);
