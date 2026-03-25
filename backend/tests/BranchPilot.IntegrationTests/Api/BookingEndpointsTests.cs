using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BranchPilot.IntegrationTests.Api;

public sealed class BookingEndpointsTests : IClassFixture<BranchPilotWebApplicationFactory>
{
    private readonly BranchPilotWebApplicationFactory _factory;

    public BookingEndpointsTests(BranchPilotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBooking_ConfirmRescheduleCancel_ExecutesLifecycle()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var bookingSetup = await CreateBookingSetupAsync(client, uniqueSuffix, ownerSession.Session.Locations.Single().Id);
        var startsAt = DateTimeOffset.UtcNow.AddDays(2);

        var createResponse = await client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequest(
                bookingSetup.Customer.Id,
                ownerSession.Session.Locations.Single().Id,
                startsAt,
                "Initial scheduled booking.",
                [new CreateBookingLineRequest(bookingSetup.Service.Id, 1)]));

        createResponse.EnsureSuccessStatusCode();

        var createdBooking = await createResponse.Content.ReadFromJsonAsync<BookingDetailsResponse>();

        Assert.NotNull(createdBooking);
        Assert.Equal("Scheduled", createdBooking.Status);

        var confirmResponse = await client.PostAsync($"/api/bookings/{createdBooking.Id}/confirm", null);
        confirmResponse.EnsureSuccessStatusCode();

        var confirmedBooking = await confirmResponse.Content.ReadFromJsonAsync<BookingDetailsResponse>();

        Assert.NotNull(confirmedBooking);
        Assert.Equal("Confirmed", confirmedBooking.Status);

        var rescheduledStart = startsAt.AddHours(3);
        var rescheduleResponse = await client.PostAsJsonAsync(
            $"/api/bookings/{createdBooking.Id}/reschedule",
            new RescheduleBookingRequest(rescheduledStart, "Customer requested a later slot."));
        rescheduleResponse.EnsureSuccessStatusCode();

        var rescheduledBooking = await rescheduleResponse.Content.ReadFromJsonAsync<BookingDetailsResponse>();

        Assert.NotNull(rescheduledBooking);
        Assert.Equal(rescheduledStart, rescheduledBooking.StartsAtUtc);
        Assert.Equal("Customer requested a later slot.", rescheduledBooking.RescheduleReason);

        var cancelResponse = await client.PostAsJsonAsync(
            $"/api/bookings/{createdBooking.Id}/cancel",
            new CancelBookingRequest("Customer is unavailable."));
        cancelResponse.EnsureSuccessStatusCode();

        var cancelledBooking = await cancelResponse.Content.ReadFromJsonAsync<BookingDetailsResponse>();

        Assert.NotNull(cancelledBooking);
        Assert.Equal("Cancelled", cancelledBooking.Status);
        Assert.Equal("Customer is unavailable.", cancelledBooking.CancellationReason);
        Assert.Contains(cancelledBooking.Timeline, entry => entry.EventName == "Cancelled");
    }

    [Fact]
    public async Task CreateBooking_WithOverlappingSlot_ReturnsConflict()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var locationId = ownerSession.Session.Locations.Single().Id;
        var bookingSetup = await CreateBookingSetupAsync(client, uniqueSuffix, locationId);
        var secondCustomer = await CreateCustomerAsync(client, $"second.{uniqueSuffix}@branchpilot.test");
        var startsAt = DateTimeOffset.UtcNow.AddDays(3);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequest(
                bookingSetup.Customer.Id,
                locationId,
                startsAt,
                "Primary booking.",
                [new CreateBookingLineRequest(bookingSetup.Service.Id, 1)]));
        firstResponse.EnsureSuccessStatusCode();

        var overlapResponse = await client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequest(
                secondCustomer.Id,
                locationId,
                startsAt.AddMinutes(15),
                "Overlapping booking.",
                [new CreateBookingLineRequest(bookingSetup.Service.Id, 1)]));

        Assert.Equal(HttpStatusCode.Conflict, overlapResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCustomer_WithExistingBooking_ReturnsConflict()
    {
        using var client = _factory.CreateApiClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];

        var ownerSession = await RegisterOrganizationAsync(client, uniqueSuffix);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ownerSession.AccessToken);

        var locationId = ownerSession.Session.Locations.Single().Id;
        var bookingSetup = await CreateBookingSetupAsync(client, uniqueSuffix, locationId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/bookings",
            new CreateBookingRequest(
                bookingSetup.Customer.Id,
                locationId,
                DateTimeOffset.UtcNow.AddDays(4),
                "Customer delete protection booking.",
                [new CreateBookingLineRequest(bookingSetup.Service.Id, 1)]));
        createResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/customers/{bookingSetup.Customer.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    private static async Task<BookingSetup> CreateBookingSetupAsync(
        HttpClient client,
        string uniqueSuffix,
        Guid locationId)
    {
        var category = await CreateCategoryAsync(client, $"Services {uniqueSuffix}");
        var taxProfile = await CreateTaxProfileAsync(client, $"VAT {uniqueSuffix}", 19m);
        var customer = await CreateCustomerAsync(client, $"customer.{uniqueSuffix}@branchpilot.test");
        var service = await CreateServiceAsync(client, uniqueSuffix, category.Id, taxProfile.Id, locationId);

        return new BookingSetup(customer, service);
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/customers",
            new CreateCustomerRequest("Booking", "Customer", email, "+40 700 000 000", "Integration customer."));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>())!;
    }

    private static async Task<CatalogItemResponse> CreateServiceAsync(
        HttpClient client,
        string uniqueSuffix,
        Guid categoryId,
        Guid taxProfileId,
        Guid locationId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/catalog/items",
            new UpsertCatalogItemRequest(
                $"Operational Visit {uniqueSuffix}",
                $"VISIT-{uniqueSuffix}".ToUpperInvariant(),
                "Service",
                categoryId,
                taxProfileId,
                "Bookable service for Stage 4 integration tests.",
                30,
                true,
                [new LocationPriceInputRequest(locationId, 45m, "EUR")],
                []));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CatalogItemResponse>())!;
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

    private sealed record RegisterOrganizationRequest(
        string TenantName,
        string PrimaryLocationName,
        string PrimaryLocationCode,
        string PrimaryLocationTimeZone,
        string FirstName,
        string LastName,
        string Email,
        string Password);

    private sealed record CreateCustomerRequest(
        string FirstName,
        string LastName,
        string Email,
        string? PhoneNumber,
        string? Notes);

    private sealed record CreateBookingRequest(
        Guid CustomerId,
        Guid LocationId,
        DateTimeOffset StartsAtUtc,
        string? Notes,
        IReadOnlyCollection<CreateBookingLineRequest> Lines);

    private sealed record CreateBookingLineRequest(Guid CatalogItemId, int Quantity);

    private sealed record RescheduleBookingRequest(DateTimeOffset StartsAtUtc, string? Reason);

    private sealed record CancelBookingRequest(string Reason);

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
        IReadOnlyCollection<object> Promotions);

    private sealed record LocationPriceInputRequest(Guid LocationId, decimal PriceAmount, string CurrencyCode);

    private sealed record AuthenticatedSessionResponse(string AccessToken, CurrentSessionResponse Session);

    private sealed record CurrentSessionResponse(IReadOnlyCollection<LocationResponse> Locations);

    private sealed record LocationResponse(Guid Id, string Name, string Code, string TimeZone);

    private sealed record CustomerResponse(Guid Id, string FirstName, string LastName, string FullName, string Email);

    private sealed record CategoryResponse(Guid Id, string Name, string Description);

    private sealed record TaxProfileResponse(Guid Id, string Name, decimal Rate);

    private sealed record CatalogItemResponse(Guid Id, string Name, string ItemType);

    private sealed record BookingDetailsResponse(
        Guid Id,
        string Number,
        string Status,
        DateTimeOffset StartsAtUtc,
        string RescheduleReason,
        string CancellationReason,
        IReadOnlyCollection<BookingTimelineEntryResponse> Timeline);

    private sealed record BookingTimelineEntryResponse(string EventName, DateTimeOffset OccurredAtUtc, string Description);

    private sealed record BookingSetup(CustomerResponse Customer, CatalogItemResponse Service);
}
