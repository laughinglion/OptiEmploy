using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IEmailVerificationTokenGenerator _tokenGenerator;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IEmailVerificationTokenGenerator tokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("A user with this email address already exists.");
        }

        var verificationToken = _tokenGenerator.GenerateToken();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Role = request.Role ?? UserRole.Requestor,
            CompanyName = request.CompanyName?.Trim(),
            CreditBalance = 0m,
            IsActive = false, // Inactive until email is verified
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Send verification email (fire-and-forget style but awaited for reliability)
        await SendVerificationEmailAsync(user, verificationToken, cancellationToken);

        return new RegisterUserResult(user.Id, user.Email, user.FullName, user.Role.ToString(), EmailVerificationRequired: true);
    }

    private async Task SendVerificationEmailAsync(User user, string token, CancellationToken cancellationToken)
    {
        var subject = "Verify your email - Employment Verify";
        var htmlBody = $"""
            <html>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>Welcome to Employment Verify, {user.FullName}!</h2>
                <p>Thank you for registering. Please verify your email address to activate your account.</p>
                <p>Your verification token is:</p>
                <div style="background-color: #f4f4f4; padding: 15px; border-radius: 5px; text-align: center; margin: 20px 0;">
                    <code style="font-size: 18px; font-weight: bold;">{token}</code>
                </div>
                <p>To confirm your email, use the following endpoint:</p>
                <p><code>GET /api/auth/confirm-email?token={token}</code></p>
                <p>This token will expire in 24 hours.</p>
                <hr />
                <p style="color: #666; font-size: 12px;">
                    This email was sent by Employment Verify. If you did not register for an account,
                    please ignore this email. Your personal information is handled in accordance with
                    the Protection of Personal Information Act (POPIA).
                </p>
            </body>
            </html>
            """;

        await _emailSender.SendEmailAsync(user.Email, subject, htmlBody, cancellationToken);
    }
}
