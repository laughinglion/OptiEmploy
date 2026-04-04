using EmploymentVerify.Domain.Enums;
using MediatR;

namespace EmploymentVerify.Application.Auth.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    string? CompanyName,
    UserRole? Role = null
) : IRequest<RegisterUserResult>;

public record RegisterUserResult(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool EmailVerificationRequired
);
