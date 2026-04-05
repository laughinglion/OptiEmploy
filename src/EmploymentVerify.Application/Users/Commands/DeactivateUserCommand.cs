using MediatR;

namespace EmploymentVerify.Application.Users.Commands;

public record DeactivateUserCommand(Guid UserId, bool IsActive) : IRequest<DeactivateUserResult>;

public record DeactivateUserResult(Guid UserId, string Email, bool IsActive);
