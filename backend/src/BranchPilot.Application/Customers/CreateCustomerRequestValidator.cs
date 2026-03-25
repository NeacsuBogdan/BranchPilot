using FluentValidation;

namespace BranchPilot.Application.Customers;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
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

        RuleFor(request => request.PhoneNumber)
            .MaximumLength(40);

        RuleFor(request => request.Notes)
            .MaximumLength(1000);
    }
}
