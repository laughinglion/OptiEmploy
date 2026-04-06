using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email, string BaseUrl) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IEmailVerificationTokenGenerator _tokenGenerator;

    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IEmailVerificationTokenGenerator tokenGenerator)
    {
        _context = context;
        _emailSender = emailSender;
        _tokenGenerator = tokenGenerator;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Always return success to prevent email enumeration
        if (user is null || !user.IsActive) return;

        var token = _tokenGenerator.GenerateToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync(cancellationToken);

        var resetLink = $"{request.BaseUrl}/account/reset-password?token={token}";
        var subject = "Reset your password — Employment Verify";
        var body = $@"
<div style=""font-family:Arial,sans-serif;font-size:15px;color:#222;max-width:600px;margin:0 auto;padding:24px;border:1px solid #ddd;border-radius:6px;"">
  <h2 style=""color:#1a56db;"">Reset Your Password</h2>
  <p>Hi {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>
  <p>We received a request to reset your Employment Verify password. Click the button below to choose a new password.</p>
  <p style=""margin:24px 0;"">
    <a href=""{resetLink}"" style=""display:inline-block;padding:12px 24px;background-color:#1a56db;color:#fff;text-decoration:none;border-radius:4px;font-weight:bold;"">Reset Password</a>
  </p>
  <p><strong>This link expires in 1 hour.</strong></p>
  <p>If you did not request a password reset, you can safely ignore this email — your password will not change.</p>
  <div style=""margin-top:32px;font-size:12px;color:#666;border-top:1px solid #eee;padding-top:16px;"">
    <p>Employment Verify — POPIA-compliant employment verification.</p>
  </div>
</div>";

        await _emailSender.SendEmailAsync(user.Email, subject, body, cancellationToken);
    }
}
