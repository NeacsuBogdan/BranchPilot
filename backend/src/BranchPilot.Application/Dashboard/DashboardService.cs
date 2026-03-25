using BranchPilot.Application.Abstractions.Persistence;
using BranchPilot.Application.Abstractions.Security;
using BranchPilot.Application.Abstractions.Time;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BranchPilot.Application.Dashboard;

public sealed class DashboardService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DashboardService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        EnsureAuthenticatedTenant();

        var now = _dateTimeProvider.UtcNow;
        var upcomingWindowEndsAt = now.AddDays(7);

        var activeCustomersTask = _dbContext.Customers
            .AsNoTracking()
            .CountAsync(customer => customer.IsActive, cancellationToken);

        var scheduledBookingsTask = _dbContext.Bookings
            .AsNoTracking()
            .CountAsync(booking => booking.Status == BookingStatus.Scheduled, cancellationToken);

        var confirmedBookingsTask = _dbContext.Bookings
            .AsNoTracking()
            .CountAsync(booking => booking.Status == BookingStatus.Confirmed, cancellationToken);

        var nextSevenDaysBookingsTask = _dbContext.Bookings
            .AsNoTracking()
            .CountAsync(
                booking =>
                    (booking.Status == BookingStatus.Scheduled || booking.Status == BookingStatus.Confirmed) &&
                    booking.StartsAtUtc >= now &&
                    booking.StartsAtUtc < upcomingWindowEndsAt,
                cancellationToken);

        var upcomingBookingsTask = (
            from booking in _dbContext.Bookings.AsNoTracking()
            join customer in _dbContext.Customers.AsNoTracking() on booking.CustomerId equals customer.Id
            join location in _dbContext.Locations.AsNoTracking() on booking.LocationId equals location.Id
            where
                (booking.Status == BookingStatus.Scheduled || booking.Status == BookingStatus.Confirmed) &&
                booking.StartsAtUtc >= now
            orderby booking.StartsAtUtc, booking.Number
            select new DashboardUpcomingBookingResponse(
                booking.Id,
                booking.Number,
                booking.Status.ToString(),
                $"{customer.FirstName} {customer.LastName}".Trim(),
                location.Name,
                booking.StartsAtUtc,
                booking.EndsAtUtc))
            .Take(5)
            .ToListAsync(cancellationToken);

        await Task.WhenAll(
            activeCustomersTask,
            scheduledBookingsTask,
            confirmedBookingsTask,
            nextSevenDaysBookingsTask,
            upcomingBookingsTask);

        return new DashboardSummaryResponse(
            activeCustomersTask.Result,
            scheduledBookingsTask.Result,
            confirmedBookingsTask.Result,
            nextSevenDaysBookingsTask.Result,
            upcomingBookingsTask.Result);
    }

    private void EnsureAuthenticatedTenant()
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.TenantId is null)
        {
            throw new AppException(401, "Authentication required", "A valid access token is required.");
        }
    }
}
