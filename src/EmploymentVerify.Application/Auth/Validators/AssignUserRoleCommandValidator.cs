using EmploymentVerify.Application.Auth.Commands;
using FluentValidation;

namespace EmploymentVerify.Application.Auth.Validators;

public class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid value (Admin, Requestor, or Operator).");
    }
}
