using EmploymentVerify.Domain.Enums;
using MediatR;

namespace EmploymentVerify.Application.Verifications.Commands;

public record RecordHrResponseCommand(
    string Token, string RespondedBy, ResponseType ResponseType,
    string? ConfirmedJobTitle, DateOnly? ConfirmedStartDate, DateOnly? ConfirmedEndDate,
    bool? IsCurrentlyEmployed, string? Notes) : IRequest<bool>;
