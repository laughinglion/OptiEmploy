using EmploymentVerify.Domain.Enums;
using MediatR;

namespace EmploymentVerify.Application.Verifications.Commands;

public record RecordOperatorCallCommand(
    Guid VerificationRequestId,
    Guid OperatorId,
    CallOutcome CallOutcome,
    string Notes,
    string? ConfirmedJobTitle,
    DateOnly? ConfirmedStartDate,
    DateOnly? ConfirmedEndDate,
    bool? IsCurrentlyEmployed,
    string? ResponseNotes) : IRequest<bool>;
