using EmploymentVerify.Application.Verifications.Commands;
using FluentValidation;

namespace EmploymentVerify.Application.Verifications.Validators;

/// <summary>
/// Validates the SubmitVerificationCommand, with particular emphasis on
/// POPIA consent requirements. Without explicit consent, submissions are rejected.
/// </summary>
public class SubmitVerificationCommandValidator : AbstractValidator<SubmitVerificationCommand>
{
    public SubmitVerificationCommandValidator()
    {
        // ── Employee Details ──

        RuleFor(x => x.EmployeeFullName)
            .NotEmpty().WithMessage("Employee full name is required.")
            .MaximumLength(200).WithMessage("Employee full name must not exceed 200 characters.");

        RuleFor(x => x.IdType)
            .NotEmpty().WithMessage("Identification type is required.")
            .Must(t => t is "SaIdNumber" or "Passport")
            .WithMessage("Identification type must be 'SaIdNumber' or 'Passport'.");

        RuleFor(x => x.SaIdNumber)
            .NotEmpty().WithMessage("SA ID Number is required when identification type is SA ID Number.")
            .When(x => x.IdType == "SaIdNumber");

        RuleFor(x => x.SaIdNumber)
            .Matches(@"^\d{13}$").WithMessage("SA ID Number must be exactly 13 digits.")
            .When(x => x.IdType == "SaIdNumber" && !string.IsNullOrEmpty(x.SaIdNumber));

        RuleFor(x => x.PassportNumber)
            .NotEmpty().WithMessage("Passport number is required when identification type is Passport.")
            .When(x => x.IdType == "Passport");

        RuleFor(x => x.PassportNumber)
            .MinimumLength(4).WithMessage("Passport number must be at least 4 characters.")
            .MaximumLength(20).WithMessage("Passport number must not exceed 20 characters.")
            .When(x => x.IdType == "Passport" && !string.IsNullOrEmpty(x.PassportNumber));

        RuleFor(x => x.PassportCountry)
            .NotEmpty().WithMessage("Passport country is required when identification type is Passport.")
            .When(x => x.IdType == "Passport");

        RuleFor(x => x.PassportCountry)
            .MaximumLength(100).WithMessage("Passport country must not exceed 100 characters.")
            .When(x => x.IdType == "Passport" && !string.IsNullOrEmpty(x.PassportCountry));

        // ── Employment Details ──

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(300).WithMessage("Company name must not exceed 300 characters.");

        RuleFor(x => x.JobTitle)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(200).WithMessage("Job title must not exceed 200 characters.");

        RuleFor(x => x.EmploymentStartDate)
            .NotEmpty().WithMessage("Employment start date is required.");

        RuleFor(x => x.EmploymentEndDate)
            .GreaterThanOrEqualTo(x => x.EmploymentStartDate)
            .WithMessage("Employment end date cannot be before the start date.")
            .When(x => x.EmploymentEndDate.HasValue);

        // ── HR Contact (optional) ──

        RuleFor(x => x.HrEmail)
            .EmailAddress().WithMessage("A valid email address is required for HR contact.")
            .MaximumLength(256).WithMessage("HR email must not exceed 256 characters.")
            .When(x => !string.IsNullOrEmpty(x.HrEmail));

        RuleFor(x => x.HrPhone)
            .MaximumLength(20).WithMessage("HR phone number must not exceed 20 characters.")
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("HR phone number contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.HrPhone));

        RuleFor(x => x.HrContactName)
            .MaximumLength(200).WithMessage("HR contact name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.HrContactName));

        // ── Consent Type ──

        RuleFor(x => x.ConsentType)
            .NotEmpty().WithMessage("Consent type is required.")
            .Must(t => t is "RequestorWarranted" or "DirectEmployee")
            .WithMessage("Consent type must be 'RequestorWarranted' or 'DirectEmployee'.");

        // ── POPIA Consent — MANDATORY ──
        // These are the critical POPIA compliance validations.
        // Without explicit consent, the request MUST be rejected.

        RuleFor(x => x.ConsentToPopia)
            .Equal(true)
            .WithMessage("POPIA consent is required. You must confirm that you have obtained the necessary consent from the individual to process their personal information for employment verification purposes.");

        RuleFor(x => x.ConsentAccuracy)
            .Equal(true)
            .WithMessage("You must confirm the accuracy of the information provided before submitting a verification request.");
    }
}
