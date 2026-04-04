using System.Security.Cryptography;
using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Commands;

public class SendVerificationEmailCommandHandler : IRequestHandler<SendVerificationEmailCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;

    public SendVerificationEmailCommandHandler(IApplicationDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    public async Task<bool> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var verification = await _context.VerificationRequests
            .FirstOrDefaultAsync(v => v.Id == request.VerificationRequestId, cancellationToken);

        if (verification is null)
            return false;

        if (string.IsNullOrWhiteSpace(verification.HrEmail))
            return false;

        // Generate URL-safe 64-char token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var token = rawToken.Replace('+', '-').Replace('/', '_').Replace("=", string.Empty);

        var emailToken = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            VerificationRequestId = request.VerificationRequestId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmailVerificationTokens.Add(emailToken);

        verification.Status = VerificationStatus.InProgress;
        verification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var confirmationLink = $"{request.BaseUrl}/verify/confirm?token={token}";
        var subject = "Employment Verification Request";
        var body = $@"<p>Dear {verification.HrContactName ?? "HR Representative"},</p>
<p>We have received an employment verification request for <strong>{verification.EmployeeFullName}</strong>
who claims to have worked at <strong>{verification.CompanyName}</strong> as <strong>{verification.JobTitle}</strong>.</p>
<p>Please click the link below to confirm or deny this information:</p>
<p><a href=""{confirmationLink}"">Respond to Verification Request</a></p>
<p>This link will expire in 48 hours.</p>
<p>If you did not expect this request or have concerns, please contact us.</p>
<p>Thank you,<br/>Employment Verify</p>";

        await _emailSender.SendEmailAsync(verification.HrEmail, subject, body, cancellationToken);

        return true;
    }
}
