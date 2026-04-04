using EmploymentVerify.Application.Companies.Commands;
using FluentValidation;

namespace EmploymentVerify.Application.Companies.Validators;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Company ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(300).WithMessage("Company name must not exceed 300 characters.");

        RuleFor(x => x.RegistrationNumber)
            .NotEmpty().WithMessage("Registration number is required.")
            .MaximumLength(50).WithMessage("Registration number must not exceed 50 characters.")
            .Matches(@"^\d{4}/\d{5,7}/\d{2}$")
            .WithMessage("Registration number must be in SA format (e.g. '2020/123456/07').");

        RuleFor(x => x.HrContactName)
            .NotEmpty().WithMessage("HR contact name is required.")
            .MaximumLength(200).WithMessage("HR contact name must not exceed 200 characters.");

        RuleFor(x => x.HrEmail)
            .NotEmpty().WithMessage("HR email is required.")
            .EmailAddress().WithMessage("A valid HR email address is required.")
            .MaximumLength(256).WithMessage("HR email must not exceed 256 characters.");

        RuleFor(x => x.HrPhone)
            .NotEmpty().WithMessage("HR phone number is required.")
            .MaximumLength(20).WithMessage("HR phone number must not exceed 20 characters.")
            .Matches(@"^(\+27|0)\d{9}$")
            .WithMessage("HR phone must be a valid South African number (e.g. '+27821234567' or '0821234567').");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.")
            .When(x => x.Address is not null);

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(x => x.City is not null);

        RuleFor(x => x.Province)
            .MaximumLength(100).WithMessage("Province must not exceed 100 characters.")
            .When(x => x.Province is not null);

        RuleFor(x => x.PostalCode)
            .MaximumLength(10).WithMessage("Postal code must not exceed 10 characters.")
            .When(x => x.PostalCode is not null);
    }
}
