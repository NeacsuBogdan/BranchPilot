using FluentValidation;

namespace BranchPilot.Application.Catalog;

public sealed class CreateTaxProfileRequestValidator : AbstractValidator<CreateTaxProfileRequest>
{
    public CreateTaxProfileRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(request => request.Rate)
            .InclusiveBetween(0m, 100m);
    }
}
