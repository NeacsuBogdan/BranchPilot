using BranchPilot.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace BranchPilot.IntegrationTests.Api;

public sealed class BranchPilotWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _databaseContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase($"branchpilot_tests_{Guid.NewGuid():N}")
        .WithUsername("branchpilot")
        .WithPassword("branchpilot")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Postgres"] = _databaseContainer.GetConnectionString(),
                        ["ConnectionStrings:Redis"] = "localhost:6379",
                        ["Infrastructure:SkipAutoInitialization"] = "true",
                    });
            });
    }

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
    }

    public async Task InitializeAsync()
    {
        await _databaseContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = _databaseContainer.GetConnectionString(),
                    ["ConnectionStrings:Redis"] = "localhost:6379",
                })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await serviceProvider.InitialiseInfrastructureAsync();
    }

    public new async Task DisposeAsync()
    {
        await _databaseContainer.DisposeAsync();
        Dispose();
    }
}
