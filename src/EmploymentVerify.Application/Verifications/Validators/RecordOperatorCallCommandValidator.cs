using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Domain.Enums;
using FluentValidation;

namespace EmploymentVerify.Application.Verifications.Validators;

public class RecordOperatorCallCommandValidator : AbstractValidator<RecordOperatorCallCommand>
{
    public RecordOperatorCallCommandValidator()
    {
        RuleFor(x => x.VerificationRequestId)
            .NotEmpty().WithMessage("Verification request ID is required.");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Call notes are required.")
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");

        RuleFor(x => x.ResponseNotes)
            .MaximumLength(2000).WithMessage("Response notes must not exceed 2000 characters.")
            .When(x => x.ResponseNotes is not null);

        RuleFor(x => x.ConfirmedJobTitle)
            .NotEmpty().WithMessage("Job title is required when confirming employment.")
            .When(x => x.CallOutcome == CallOutcome.Confirmed);

        RuleFor(x => x.ConfirmedStartDate)
            .NotEmpty().WithMessage("Start date is required when confirming employment.")
            .When(x => x.CallOutcome == CallOutcome.Confirmed);
    }
}
