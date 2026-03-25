using BranchPilot.Application.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Bookings;

public static class BookingBusinessRules
{
    public static void EnsureHasLines(IReadOnlyCollection<CreateBookingLineRequest> lines)
    {
        if (lines.Count == 0)
        {
            throw new AppException(400, "Booking lines required", "A booking must contain at least one service line.");
        }
    }

    public static void EnsureDistinctCatalogItems(IReadOnlyCollection<CreateBookingLineRequest> lines)
    {
        if (lines.Select(line => line.CatalogItemId).Distinct().Count() != lines.Count)
        {
            throw new AppException(
                409,
                "Duplicate booking lines",
                "Each service can appear only once within the same booking.");
        }
    }

    public static void EnsureStartsInFuture(DateTimeOffset startsAtUtc, DateTimeOffset now)
    {
        if (startsAtUtc <= now)
        {
            throw new AppException(
                409,
                "Booking start must be in the future",
                "Bookings must be scheduled in the future.");
        }
    }

    public static void EnsureSingleCurrency(IReadOnlyCollection<string> currencyCodes)
    {
        if (currencyCodes.Select(code => code.ToUpperInvariant()).Distinct().Count() > 1)
        {
            throw new AppException(
                409,
                "Mixed booking currencies are not supported",
                "All selected services must use the same currency at the chosen location.");
        }
    }

    public static void EnsureCanConfirm(BookingStatus status)
    {
        if (status != BookingStatus.Scheduled)
        {
            throw new AppException(
                409,
                "Booking cannot be confirmed",
                "Only scheduled bookings can be confirmed.");
        }
    }

    public static void EnsureCanComplete(BookingStatus status)
    {
        if (status is not BookingStatus.Scheduled and not BookingStatus.Confirmed)
        {
            throw new AppException(
                409,
                "Booking cannot be completed",
                "Only scheduled or confirmed bookings can be completed.");
        }
    }

    public static void EnsureCanReschedule(BookingStatus status)
    {
        if (status is BookingStatus.Completed or BookingStatus.Cancelled)
        {
            throw new AppException(
                409,
                "Booking cannot be rescheduled",
                "Completed or cancelled bookings cannot be rescheduled.");
        }
    }

    public static void EnsureCanCancel(BookingStatus status)
    {
        if (status is BookingStatus.Completed or BookingStatus.Cancelled)
        {
            throw new AppException(
                409,
                "Booking cannot be cancelled",
                "Completed or cancelled bookings cannot be cancelled.");
        }
    }
}
