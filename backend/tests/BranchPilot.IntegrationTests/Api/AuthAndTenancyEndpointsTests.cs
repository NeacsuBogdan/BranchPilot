using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BranchPilot.IntegrationTests.Api;

public sealed class AuthAndTenancyEndpointsTests : IClassFixture<BranchPilotWebApplicationFactory>
{
    private readonly BranchPilotWebApplicationFactory _factory;

    public AuthAndTenancyEndpointsTests(BranchPilotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithSeededDemoUser_ReturnsAuthenticatedSession()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("owner@branchpilot.demo", "BranchPilot!123"));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthenticatedSessionResponse>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Equal("Northwind Operations Group", payload.Session.Tenant.Name);
        Assert.Equal(2, payload.Session.Locations.Count);
    }

    [Fact]
    public async Task RegisterOrganization_CreatesTenantOwnerAndPrimaryLocation()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var response = await client.PostAsJsonAsync(
            "/api/auth/register-organization",
            new RegisterOrganizationRequest(
                $"Harbor Pine {uniqueSuffix}",
                "Bucharest Flagship",
                $"B{uniqueSuffix}".ToUpperInvariant(),
                "Europe/Bucharest",
                "Elena",
                "Ionescu",
                $"elena.{uniqueSuffix}@branchpilot.test",
                "BranchPilot!123"));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthenticatedSessionResponse>();

        Assert.NotNull(payload);
        Assert.Equal($"Harbor Pine {uniqueSuffix}", payload.Session.Tenant.Name);
        Assert.Equal("Elena Ionescu", payload.Session.User.FullName);
        Assert.Single(payload.Session.Locations);
    }

    [Fact]
    public async Task CreateLocation_WithAuthenticatedTenant_AddsLocationToCurrentTenant()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var registeredSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", registeredSession.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/locations",
            new CreateLocationRequest("Timisoara West", $"T{uniqueSuffix}".ToUpperInvariant(), "Europe/Bucharest"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var getLocationsResponse = await client.GetAsync("/api/locations");
        getLocationsResponse.EnsureSuccessStatusCode();

        var locations = await getLocationsResponse.Content.ReadFromJsonAsync<List<LocationResponse>>();

        Assert.NotNull(locations);
        Assert.Equal(2, locations.Count);
        Assert.Contains(locations, location => location.Name == "Timisoara West");
    }

    [Fact]
    public async Task GetLocations_DoesNotLeakAcrossTenants()
    {
        using var ownerClient = _factory.CreateApiClient();
        using var secondTenantClient = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await LoginAsync(ownerClient, "owner@branchpilot.demo", "BranchPilot!123");
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var secondTenantSession = await RegisterOrganizationAsync(secondTenantClient, uniqueSuffix);
        secondTenantClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondTenantSession.AccessToken);

        var ownerLocationCode = $"O{uniqueSuffix}".ToUpperInvariant();
        var ownerLocationName = $"Owner Expansion {uniqueSuffix}";

        var ownerCreateLocationResponse = await ownerClient.PostAsJsonAsync(
            "/api/locations",
            new CreateLocationRequest(ownerLocationName, ownerLocationCode, "Europe/Bucharest"));

        ownerCreateLocationResponse.EnsureSuccessStatusCode();

        var secondTenantLocationsResponse = await secondTenantClient.GetAsync("/api/locations");
        secondTenantLocationsResponse.EnsureSuccessStatusCode();

        var secondTenantLocations =
            await secondTenantLocationsResponse.Content.ReadFromJsonAsync<List<LocationResponse>>();

        Assert.NotNull(secondTenantLocations);
        Assert.Single(secondTenantLocations);
        Assert.DoesNotContain(secondTenantLocations, location => location.Code == ownerLocationCode);
        Assert.DoesNotContain(secondTenantLocations, location => location.Name == ownerLocationName);
    }

    [Fact]
    public async Task GetLiveHealth_ReturnsHealthyStatus()
    {
        using var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":\"Healthy\"", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<AuthenticatedSessionResponse> LoginAsync(
        HttpClient client,
        string email,
        string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthenticatedSessionResponse>())!;
    }

    private static async Task<AuthenticatedSessionResponse> RegisterOrganizationAsync(
        HttpClient client,
        string uniqueSuffix)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register-organization",
            new RegisterOrganizationRequest(
                $"Tenant {uniqueSuffix}",
                "Primary Hub",
                $"P{uniqueSuffix}".ToUpperInvariant(),
                "Europe/Bucharest",
                "Stage",
                "Owner",
                $"owner.{uniqueSuffix}@branchpilot.test",
                "BranchPilot!123"));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthenticatedSessionResponse>())!;
    }

    private sealed record LoginRequest(string Email, string Password);

    private sealed record RegisterOrganizationRequest(
        string TenantName,
        string PrimaryLocationName,
        string PrimaryLocationCode,
        string PrimaryLocationTimeZone,
        string FirstName,
        string LastName,
        string Email,
        string Password);

    private sealed record CreateLocationRequest(string Name, string Code, string TimeZone);

    private sealed record AuthenticatedSessionResponse(
        string AccessToken,
        string RefreshToken,
        CurrentSessionResponse Session);

    private sealed record CurrentSessionResponse(
        UserSummaryResponse User,
        TenantSummaryResponse Tenant,
        IReadOnlyCollection<LocationResponse> Locations);

    private sealed record UserSummaryResponse(string FullName);

    private sealed record TenantSummaryResponse(string Name);

    private sealed record LocationResponse(string Name, string Code, string TimeZone);
}
