using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ResetPasswordResult>;
public record ResetPasswordResult(bool Success, string? ErrorMessage);

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token, cancellationToken);

        if (user is null)
            return new ResetPasswordResult(false, "Invalid or expired reset token.");

        if (user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return new ResetPasswordResult(false, "This reset link has expired. Please request a new one.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        // Reset lockout on successful password change
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "PasswordReset",
            Description = "User reset their password via email link.",
            ActorUserId = user.Id,
            TargetUserId = user.Id,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return new ResetPasswordResult(true, null);
    }
}
