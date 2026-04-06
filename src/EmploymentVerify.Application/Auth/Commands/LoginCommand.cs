using MediatR;

namespace EmploymentVerify.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(
    bool Success,
    string? Token,
    string? RefreshToken,
    Guid? UserId,
    string? Email,
    string? FullName,
    string? Role,
    string? ErrorMessage);
