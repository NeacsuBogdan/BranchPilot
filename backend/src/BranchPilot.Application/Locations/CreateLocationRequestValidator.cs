using FluentValidation;

namespace BranchPilot.Application.Locations;

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(12)
            .Matches("^[A-Za-z0-9-]{2,12}$");

        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .MaximumLength(100);
    }
}
