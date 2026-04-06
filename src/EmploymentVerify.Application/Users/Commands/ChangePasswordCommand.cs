using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Commands;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : IRequest<ChangePasswordResult>;

public record ChangePasswordResult(bool Success, string? ErrorMessage);

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return new ChangePasswordResult(false, "User not found.");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return new ChangePasswordResult(false, "Current password is incorrect.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "PasswordChanged",
            Description = "User changed their password.",
            ActorUserId = request.UserId,
            TargetUserId = request.UserId,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResult(true, null);
    }
}
