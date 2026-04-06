using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Commands;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, DeactivateUserResult>
{
    private readonly IApplicationDbContext _context;

    public DeactivateUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DeactivateUserResult> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new InvalidOperationException($"User with ID '{request.UserId}' was not found.");

        user.IsActive = request.IsActive;

        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = request.IsActive ? "UserReactivated" : "UserDeactivated",
            Description = $"User {user.Email} was {(request.IsActive ? "reactivated" : "deactivated")} by admin.",
            TargetUserId = user.Id,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new DeactivateUserResult(user.Id, user.Email, user.IsActive);
    }
}
