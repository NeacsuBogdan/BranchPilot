using FluentValidation;

namespace BranchPilot.Application.Bookings;

public sealed class RescheduleBookingRequestValidator : AbstractValidator<RescheduleBookingRequest>
{
    public RescheduleBookingRequestValidator()
    {
        RuleFor(request => request.StartsAtUtc)
            .Must(value => value != default)
            .WithMessage("A new booking start time is required.");

        RuleFor(request => request.Reason)
            .MaximumLength(300);
    }
}
