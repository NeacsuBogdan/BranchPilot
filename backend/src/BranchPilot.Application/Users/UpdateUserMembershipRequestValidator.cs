using BranchPilot.Application.Security;
using FluentValidation;

namespace BranchPilot.Application.Users;

public sealed class UpdateUserMembershipRequestValidator : AbstractValidator<UpdateUserMembershipRequest>
{
    public UpdateUserMembershipRequestValidator()
    {
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
