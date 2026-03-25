using BranchPilot.Application.Bookings;
using BranchPilot.Application.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.UnitTests.Bookings;

public sealed class BookingBusinessRulesTests
{
    [Fact]
    public void EnsureHasLines_WithNoLines_Throws()
    {
        var exception = Assert.Throws<AppException>(
            () => BookingBusinessRules.EnsureHasLines(Array.Empty<CreateBookingLineRequest>()));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void EnsureDistinctCatalogItems_WithDuplicateServices_Throws()
    {
        var serviceId = Guid.NewGuid();
        CreateBookingLineRequest[] lines =
        [
            new CreateBookingLineRequest(serviceId, 1),
            new CreateBookingLineRequest(serviceId, 2),
        ];

        var exception = Assert.Throws<AppException>(() => BookingBusinessRules.EnsureDistinctCatalogItems(lines));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public void EnsureStartsInFuture_WithPastStart_Throws()
    {
        var now = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero);
        var start = now.AddMinutes(-15);

        var exception = Assert.Throws<AppException>(() => BookingBusinessRules.EnsureStartsInFuture(start, now));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public void EnsureCanConfirm_WithCancelledStatus_Throws()
    {
        var exception = Assert.Throws<AppException>(() => BookingBusinessRules.EnsureCanConfirm(BookingStatus.Cancelled));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public void EnsureCanCancel_WithCompletedStatus_Throws()
    {
        var exception = Assert.Throws<AppException>(() => BookingBusinessRules.EnsureCanCancel(BookingStatus.Completed));

        Assert.Equal(409, exception.StatusCode);
    }
}
