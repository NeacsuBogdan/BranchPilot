using BranchPilot.Application.Security;
using FluentValidation;

namespace BranchPilot.Application.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(100);

        RuleFor(request => request.Role)
            .NotEmpty()
            .Must((role) => MembershipRoleParser.TryParse(role, out _))
            .WithMessage("A supported role is required.");

        RuleFor(request => request.LocationIds)
            .NotEmpty()
            .Must((locationIds) => locationIds.Distinct().Count() == locationIds.Count)
            .WithMessage("Assigned locations must be unique.");
    }
}
