using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Domain.Enums;
using FluentValidation;

namespace EmploymentVerify.Application.Verifications.Validators;

public class RecordHrResponseCommandValidator : AbstractValidator<RecordHrResponseCommand>
{
    public RecordHrResponseCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");

        RuleFor(x => x.RespondedBy)
            .NotEmpty().WithMessage("Responder name is required.")
            .MaximumLength(200).WithMessage("Responder name must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.ConfirmedJobTitle)
            .NotEmpty().WithMessage("Job title is required when confirming employment.")
            .When(x => x.ResponseType == ResponseType.Confirmed);

        RuleFor(x => x.ConfirmedStartDate)
            .NotEmpty().WithMessage("Start date is required when confirming employment.")
            .When(x => x.ResponseType == ResponseType.Confirmed);
    }
}
