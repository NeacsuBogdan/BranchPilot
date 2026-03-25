using FluentValidation;

namespace BranchPilot.Application.Auth;

public sealed class RegisterOrganizationRequestValidator : AbstractValidator<RegisterOrganizationRequest>
{
    public RegisterOrganizationRequestValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.PrimaryLocationName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(x => x.PrimaryLocationCode)
            .NotEmpty()
            .MaximumLength(12)
            .Matches("^[A-Za-z0-9-]{2,12}$");

        RuleFor(x => x.PrimaryLocationTimeZone)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);
    }
}
