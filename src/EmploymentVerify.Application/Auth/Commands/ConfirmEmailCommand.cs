using MediatR;

namespace EmploymentVerify.Application.Auth.Commands;

public record ConfirmEmailCommand(string Token) : IRequest<ConfirmEmailResult>;

public record ConfirmEmailResult(
    Guid UserId,
    string Email,
    bool Success,
    string Message
);
