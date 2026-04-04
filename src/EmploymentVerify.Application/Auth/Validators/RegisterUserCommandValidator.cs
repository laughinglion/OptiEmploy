using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Domain.Enums;
using FluentValidation;

namespace EmploymentVerify.Application.Auth.Validators;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.")
            .When(x => x.CompanyName is not null);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid value (Admin, Requestor, or Operator).")
            .When(x => x.Role is not null);
    }
}
