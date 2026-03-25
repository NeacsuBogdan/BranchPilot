using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BranchPilot.IntegrationTests.Api;

public sealed class CatalogEndpointsTests : IClassFixture<BranchPilotWebApplicationFactory>
{
    private readonly BranchPilotWebApplicationFactory _factory;

    public CatalogEndpointsTests(BranchPilotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCatalogItems_AsOwner_ReturnsSeededCatalogPage()
    {
        using var client = _factory.CreateApiClient();

        var ownerSession = await LoginAsync(client, "owner@branchpilot.demo", "BranchPilot!123");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var response = await client.GetAsync("/api/catalog/items?page=1&pageSize=10&search=Consult");

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CatalogItemPageResponse>();

        Assert.NotNull(payload);
        Assert.True(payload.TotalCount >= 1);
        Assert.Contains(payload.Items, item => item.Code == "CONSULT-PREMIUM");
    }

    [Fact]
    public async Task CreateCatalogItem_WithValidPayload_CreatesScopedItem()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var category = await CreateCategoryAsync(client, "Services");
        var taxProfile = await CreateTaxProfileAsync(client, "VAT 19", 19m);

        var createLocationResponse = await client.PostAsJsonAsync(
            "/api/locations",
            new CreateLocationRequest("Timisoara West", $"T{uniqueSuffix}".ToUpperInvariant(), "Europe/Bucharest"));
        createLocationResponse.EnsureSuccessStatusCode();

        var session = await client.GetFromJsonAsync<CurrentSessionResponse>("/api/auth/me");
        Assert.NotNull(session);
        Assert.Equal(2, session.Locations.Count);

        var locationIds = session.Locations.Select(location => location.Id).ToArray();
        var response = await client.PostAsJsonAsync(
            "/api/catalog/items",
            new UpsertCatalogItemRequest(
                "Operational Check-in",
                $"CHECKIN-{uniqueSuffix}".ToUpperInvariant(),
                "Service",
                category.Id,
                taxProfile.Id,
                "Stage 3 integration flow item.",
                30,
                true,
                locationIds.Select(locationId => new LocationPriceInputRequest(locationId, 35m, "EUR")).ToArray(),
                [new PromotionInputRequest("Launch", locationIds[0], 10m, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7))]));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CatalogItemDetailsResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Operational Check-in", payload.Name);
        Assert.Equal("Service", payload.ItemType);
        Assert.Equal(2, payload.LocationPrices.Count);
        Assert.Single(payload.Promotions);
    }

    [Fact]
    public async Task CreateCatalogItem_WithOverlappingPromotions_ReturnsConflict()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var category = await CreateCategoryAsync(client, "Services");
        var taxProfile = await CreateTaxProfileAsync(client, "VAT 19", 19m);
        var session = await client.GetFromJsonAsync<CurrentSessionResponse>("/api/auth/me");

        Assert.NotNull(session);

        var locationId = session.Locations.Single().Id;
        var startsAt = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync(
            "/api/catalog/items",
            new UpsertCatalogItemRequest(
                "Launch Visit",
                $"VISIT-{uniqueSuffix}".ToUpperInvariant(),
                "Service",
                category.Id,
                taxProfile.Id,
                "Promotion overlap validation path.",
                25,
                true,
                [new LocationPriceInputRequest(locationId, 32m, "EUR")],
                [
                    new PromotionInputRequest("Wave 1", locationId, 10m, startsAt, startsAt.AddDays(4)),
                    new PromotionInputRequest("Wave 2", locationId, 15m, startsAt.AddDays(3), startsAt.AddDays(8)),
                ]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsStaff_ReturnsForbidden()
    {
        using var ownerClient = _factory.CreateApiClient();
        using var staffClient = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(ownerClient, uniqueSuffix);
        ownerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var createUserResponse = await ownerClient.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                "Catalog",
                "Staff",
                $"catalog.staff.{uniqueSuffix}@branchpilot.test",
                "BranchPilot!123",
                "Staff",
                ownerSession.Session.Locations.Select(location => location.Id).ToArray()));

        createUserResponse.EnsureSuccessStatusCode();

        var staffSession = await LoginAsync(
            staffClient,
            $"catalog.staff.{uniqueSuffix}@branchpilot.test",
            "BranchPilot!123");
        staffClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", staffSession.AccessToken);

        var response = await staffClient.PostAsJsonAsync(
            "/api/catalog/categories",
            new CreateCategoryRequest("Blocked category", "Should not be allowed."));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/catalog/categories",
            new CreateCategoryRequest(name, $"{name} catalog grouping."));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    private static async Task<TaxProfileResponse> CreateTaxProfileAsync(HttpClient client, string name, decimal rate)
    {
        var response = await client.PostAsJsonAsync(
            "/api/catalog/tax-profiles",
            new CreateTaxProfileRequest(name, rate));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaxProfileResponse>())!;
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

    private sealed record CreateCategoryRequest(string Name, string? Description);

    private sealed record CreateTaxProfileRequest(string Name, decimal Rate);

    private sealed record UpsertCatalogItemRequest(
        string Name,
        string Code,
        string ItemType,
        Guid? CategoryId,
        Guid TaxProfileId,
        string? Description,
        int? DurationInMinutes,
        bool IsActive,
        IReadOnlyCollection<LocationPriceInputRequest> LocationPrices,
        IReadOnlyCollection<PromotionInputRequest> Promotions);

    private sealed record LocationPriceInputRequest(Guid LocationId, decimal PriceAmount, string CurrencyCode);

    private sealed record PromotionInputRequest(
        string Name,
        Guid LocationId,
        decimal DiscountPercentage,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc);

    private sealed record AuthenticatedSessionResponse(string AccessToken, CurrentSessionResponse Session);

    private sealed record CurrentSessionResponse(IReadOnlyCollection<LocationResponse> Locations);

    private sealed record LocationResponse(Guid Id, string Name, string Code, string TimeZone);

    private sealed record CategoryResponse(Guid Id, string Name, string Description);

    private sealed record TaxProfileResponse(Guid Id, string Name, decimal Rate);

    private sealed record CatalogItemPageResponse(
        IReadOnlyCollection<CatalogItemSummaryResponse> Items,
        int Page,
        int PageSize,
        int TotalCount);

    private sealed record CatalogItemSummaryResponse(Guid Id, string Name, string Code, string ItemType);

    private sealed record CatalogItemDetailsResponse(
        Guid Id,
        string Name,
        string ItemType,
        IReadOnlyCollection<LocationPriceResponse> LocationPrices,
        IReadOnlyCollection<PromotionResponse> Promotions);

    private sealed record LocationPriceResponse(Guid LocationId, string LocationName, string LocationCode, decimal PriceAmount, string CurrencyCode);

    private sealed record PromotionResponse(
        Guid Id,
        string Name,
        Guid LocationId,
        string LocationName,
        string LocationCode,
        decimal DiscountPercentage,
        DateTimeOffset StartsAtUtc,
        DateTimeOffset EndsAtUtc,
        bool IsActive);
}
