using MediatR;

namespace EmploymentVerify.Application.Users.Commands;

public record UpdateUserCreditCommand(Guid UserId, decimal Amount, string Reason) : IRequest<decimal>;
// Returns new balance. Use positive Amount to add, negative to deduct. Throws if would go negative.
