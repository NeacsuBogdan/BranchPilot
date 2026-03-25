using BranchPilot.Application.Common;

namespace BranchPilot.Application.Bookings;

public sealed record GetBookingsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    Guid? LocationId = null,
    string? Status = null);

public sealed record CreateBookingRequest(
    Guid CustomerId,
    Guid LocationId,
    DateTimeOffset StartsAtUtc,
    string? Notes,
    IReadOnlyCollection<CreateBookingLineRequest> Lines);

public sealed record CreateBookingLineRequest(Guid CatalogItemId, int Quantity);

public sealed record RescheduleBookingRequest(DateTimeOffset StartsAtUtc, string? Reason);

public sealed record CancelBookingRequest(string Reason);

public sealed record BookingCustomerOptionResponse(Guid Id, string FullName, string Email);

public sealed record BookingLocationOptionResponse(Guid Id, string Name, string Code, string TimeZone);

public sealed record BookingServicePriceOptionResponse(Guid LocationId, decimal PriceAmount, string CurrencyCode);

public sealed record BookingServiceOptionResponse(
    Guid Id,
    string Name,
    string Code,
    int DurationInMinutes,
    IReadOnlyCollection<BookingServicePriceOptionResponse> LocationPrices);

public sealed record BookingOptionsResponse(
    IReadOnlyCollection<BookingCustomerOptionResponse> Customers,
    IReadOnlyCollection<BookingLocationOptionResponse> Locations,
    IReadOnlyCollection<BookingServiceOptionResponse> Services);

public sealed record BookingCustomerSummaryResponse(Guid Id, string FullName, string Email);

public sealed record BookingLocationSummaryResponse(Guid Id, string Name, string Code);

public sealed record BookingLineResponse(
    Guid Id,
    Guid CatalogItemId,
    string ItemName,
    int Quantity,
    int DurationInMinutes,
    decimal UnitPriceAmount,
    decimal LineTotalAmount);

public sealed record BookingTimelineEntryResponse(string EventName, DateTimeOffset OccurredAtUtc, string Description);

public sealed record BookingListItemResponse(
    Guid Id,
    string Number,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Notes,
    BookingCustomerSummaryResponse Customer,
    BookingLocationSummaryResponse Location,
    decimal TotalAmount,
    string CurrencyCode,
    int TotalDurationInMinutes,
    int LineCount);

public sealed record BookingDetailsResponse(
    Guid Id,
    string Number,
    string Status,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Notes,
    BookingCustomerSummaryResponse Customer,
    BookingLocationSummaryResponse Location,
    decimal TotalAmount,
    string CurrencyCode,
    int TotalDurationInMinutes,
    string CancellationReason,
    string RescheduleReason,
    IReadOnlyCollection<BookingLineResponse> Lines,
    IReadOnlyCollection<BookingTimelineEntryResponse> Timeline);

public sealed record BookingPageResponse(
    IReadOnlyCollection<BookingListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount) : PagedResponse<BookingListItemResponse>(Items, Page, PageSize, TotalCount);
