using BranchPilot.Domain.Common;
using BranchPilot.Domain.Enums;

namespace BranchPilot.Domain.Entities;

public sealed class Booking : ITenantEntity
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid LocationId { get; set; }

    public Guid CustomerId { get; set; }

    public string Number { get; set; } = string.Empty;

    public BookingStatus Status { get; set; }

    public DateTimeOffset StartsAtUtc { get; set; }

    public DateTimeOffset EndsAtUtc { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int TotalDurationInMinutes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ConfirmedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset? CancelledAtUtc { get; set; }

    public DateTimeOffset? RescheduledAtUtc { get; set; }

    public string CancellationReason { get; set; } = string.Empty;

    public string RescheduleReason { get; set; } = string.Empty;
}
