using EmploymentVerify.Domain.Enums;
using MediatR;

namespace EmploymentVerify.Application.Auth.Commands;

public record AssignUserRoleCommand(
    Guid UserId,
    UserRole Role
) : IRequest<AssignUserRoleResult>;

public record AssignUserRoleResult(
    Guid UserId,
    string Email,
    string PreviousRole,
    string NewRole
);
