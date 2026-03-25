using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BranchPilot.IntegrationTests.Api;

public sealed class RoleAndMembershipEndpointsTests : IClassFixture<BranchPilotWebApplicationFactory>
{
    private readonly BranchPilotWebApplicationFactory _factory;

    public RoleAndMembershipEndpointsTests(BranchPilotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_AsOwner_ReturnsTeamRosterWithRoles()
    {
        using var client = _factory.CreateApiClient();

        var ownerSession = await LoginAsync(client, "owner@branchpilot.demo", "BranchPilot!123");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var response = await client.GetAsync("/api/users?page=1&pageSize=10");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<TeamMemberResponse>>();

        Assert.NotNull(payload);
        Assert.True(payload.TotalCount >= 2);
        Assert.Contains(payload.Items, item => item.Membership.Role == "Owner");
        Assert.Contains(payload.Items, item => item.Membership.Role == "Admin");
    }

    [Fact]
    public async Task GetUsers_AsStaff_ReturnsForbidden()
    {
        using var ownerClient = _factory.CreateApiClient();
        using var staffClient = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(ownerClient, uniqueSuffix);
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var createdUserResponse = await ownerClient.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Mira",
                "Stone",
                $"staff.{uniqueSuffix}@branchpilot.test",
                "BranchPilot!123",
                "Staff",
                ownerSession.Session.Locations.Select(location => location.Id).ToArray()));

        createdUserResponse.EnsureSuccessStatusCode();

        var staffSession = await LoginAsync(
            staffClient,
            $"staff.{uniqueSuffix}@branchpilot.test",
            "BranchPilot!123");
        staffClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", staffSession.AccessToken);

        var staffResponse = await staffClient.GetAsync("/api/users?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, staffResponse.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AssignsRoleAndLocations()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var secondLocationResponse = await client.PostAsJsonAsync(
            "/api/locations",
            new CreateLocationRequest("Timisoara West", $"T{uniqueSuffix}".ToUpperInvariant(), "Europe/Bucharest"));

        secondLocationResponse.EnsureSuccessStatusCode();

        var sessionResponse = await client.GetFromJsonAsync<CurrentSessionResponse>("/api/auth/me");

        Assert.NotNull(sessionResponse);
        Assert.Equal(2, sessionResponse.Locations.Count);

        var createUserResponse = await client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Casey",
                "Hart",
                $"manager.{uniqueSuffix}@branchpilot.test",
                "BranchPilot!123",
                "Manager",
                sessionResponse.Locations.Select(location => location.Id).ToArray()));

        createUserResponse.EnsureSuccessStatusCode();

        var createdUser = await createUserResponse.Content.ReadFromJsonAsync<TeamMemberResponse>();

        Assert.NotNull(createdUser);
        Assert.Equal("Manager", createdUser.Membership.Role);
        Assert.Equal(2, createdUser.Membership.AssignedLocations.Count);
    }

    [Fact]
    public async Task UpdateMembership_CannotDemoteLastOwner()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var rosterResponse = await client.GetFromJsonAsync<PagedResponse<TeamMemberResponse>>(
            "/api/users?page=1&pageSize=10");

        Assert.NotNull(rosterResponse);

        var owner = Assert.Single(rosterResponse.Items, item => item.Membership.Role == "Owner");

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/users/{owner.Id}/membership",
            new UpdateUserMembershipRequest(
                "Staff",
                true,
                owner.Membership.AssignedLocations.Select(location => location.Id).ToArray()));

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
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

    private sealed record CreateUserRequest(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role,
        IReadOnlyCollection<Guid> LocationIds);

    private sealed record UpdateUserMembershipRequest(
        string Role,
        bool IsActive,
        IReadOnlyCollection<Guid> LocationIds);

    private sealed record AuthenticatedSessionResponse(
        string AccessToken,
        CurrentSessionResponse Session);

    private sealed record CurrentSessionResponse(
        IReadOnlyCollection<LocationResponse> Locations);

    private sealed record LocationResponse(Guid Id, string Name, string Code, string TimeZone);

    private sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);

    private sealed record TeamMemberResponse(
        Guid Id,
        MembershipResponse Membership);

    private sealed record MembershipResponse(
        string Role,
        IReadOnlyCollection<AssignedLocationResponse> AssignedLocations);

    private sealed record AssignedLocationResponse(Guid Id, string Name, string Code);
}
