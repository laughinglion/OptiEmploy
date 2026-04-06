using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Users.Commands;

/// <summary>
/// POPIA Article 24 — Right to Erasure (Right to be Forgotten).
/// Anonymises personal data rather than hard-deleting to preserve audit trail integrity.
/// Verification history is retained but de-identified.
/// </summary>
public record DeleteMyAccountCommand(Guid UserId, string Password) : IRequest<DeleteMyAccountResult>;
public record DeleteMyAccountResult(bool Success, string? ErrorMessage);

public class DeleteMyAccountCommandHandler : IRequestHandler<DeleteMyAccountCommand, DeleteMyAccountResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DeleteMyAccountCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<DeleteMyAccountResult> Handle(DeleteMyAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
            return new DeleteMyAccountResult(false, "User not found.");

        // Require password confirmation (SSO users have empty PasswordHash — skip check)
        if (!string.IsNullOrEmpty(user.PasswordHash) &&
            !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return new DeleteMyAccountResult(false, "Password confirmation failed.");

        // Anonymise PII — preserve Id and audit records but remove identifying data
        var anonymisedEmail = $"deleted_{user.Id:N}@anonymised.invalid";
        user.Email = anonymisedEmail;
        user.FullName = "Deleted User";
        user.CompanyName = null;
        user.PhoneNumber = null;
        user.PasswordHash = string.Empty;
        user.EmailVerificationToken = null;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;
        user.IsActive = false;

        // Revoke all refresh tokens
        await _context.RefreshTokens
            .Where(t => t.UserId == request.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), cancellationToken);

        _context.AuditEvents.Add(new Domain.Entities.AuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "AccountDeleted",
            Description = "User account anonymised under POPIA right to erasure.",
            ActorUserId = request.UserId,
            TargetUserId = request.UserId,
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return new DeleteMyAccountResult(true, null);
    }
}
