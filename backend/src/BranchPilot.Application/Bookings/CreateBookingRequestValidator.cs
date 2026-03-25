using FluentValidation;

namespace BranchPilot.Application.Bookings;

public sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(request => request.CustomerId)
            .NotEmpty();

        RuleFor(request => request.LocationId)
            .NotEmpty();

        RuleFor(request => request.StartsAtUtc)
            .Must(value => value != default)
            .WithMessage("A booking start time is required.");

        RuleFor(request => request.Notes)
            .MaximumLength(1000);

        RuleFor(request => request.Lines)
            .NotEmpty()
            .Must(lines => lines.Select(line => line.CatalogItemId).Distinct().Count() == lines.Count)
            .WithMessage("Each service can appear only once within the same booking.");

        RuleForEach(request => request.Lines)
            .ChildRules(
                line =>
                {
                    line.RuleFor(item => item.CatalogItemId)
                        .NotEmpty();

                    line.RuleFor(item => item.Quantity)
                        .InclusiveBetween(1, 10);
                });
    }
}
