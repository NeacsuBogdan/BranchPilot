using System.Net;
using System.Net.Http.Json;

namespace BranchPilot.IntegrationTests.Api;

public sealed class PlatformEndpointsTests : IClassFixture<BranchPilotWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformEndpointsTests(BranchPilotWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPlatformInfo_ReturnsBootstrapMetadata()
    {
        var response = await _client.GetAsync("/api/platform/info");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PlatformInfoResponse>();

        Assert.NotNull(payload);
        Assert.Equal("BranchPilot API", payload.Name);
        Assert.Equal("/swagger", payload.Documentation);
        Assert.Contains("/health/live", payload.HealthEndpoints);
    }

    [Fact]
    public async Task GetLiveHealth_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":\"Healthy\"", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record PlatformInfoResponse(
        string Name,
        string Environment,
        string Documentation,
        string[] HealthEndpoints);
}
