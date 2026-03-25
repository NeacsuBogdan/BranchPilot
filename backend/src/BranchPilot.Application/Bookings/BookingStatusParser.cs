using BranchPilot.Domain.Enums;

namespace BranchPilot.Application.Bookings;

public static class BookingStatusParser
{
    public static bool TryParse(string? value, out BookingStatus status)
    {
        if (Enum.TryParse<BookingStatus>(value?.Trim(), true, out status))
        {
            return true;
        }

        status = default;
        return false;
    }
}
