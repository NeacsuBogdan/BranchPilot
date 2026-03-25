namespace BranchPilot.Application.Dashboard;

public sealed record DashboardUpcomingBookingResponse(
    Guid Id,
    string Number,
    string Status,
    string CustomerName,
    string LocationName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);

public sealed record DashboardSummaryResponse(
    int ActiveCustomers,
    int ScheduledBookings,
    int ConfirmedBookings,
    int NextSevenDaysBookings,
    IReadOnlyCollection<DashboardUpcomingBookingResponse> UpcomingBookings);
