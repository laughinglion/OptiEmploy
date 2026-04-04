using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResult>
{
    private readonly IApplicationDbContext _context;

    public ConfirmEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ConfirmEmailResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new ConfirmEmailResult(Guid.Empty, string.Empty, false, "Verification token is required.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token, cancellationToken);

        if (user is null)
        {
            return new ConfirmEmailResult(Guid.Empty, string.Empty, false, "Invalid verification token.");
        }

        if (user.IsEmailVerified)
        {
            return new ConfirmEmailResult(user.Id, user.Email, false, "Email has already been verified.");
        }

        if (user.EmailVerificationTokenExpiresAt.HasValue && user.EmailVerificationTokenExpiresAt.Value < DateTime.UtcNow)
        {
            return new ConfirmEmailResult(user.Id, user.Email, false, "Verification token has expired. Please request a new verification email.");
        }

        user.IsEmailVerified = true;
        user.IsActive = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);

        return new ConfirmEmailResult(user.Id, user.Email, true, "Email verified successfully. Your account is now active.");
    }
}
