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
        var body = BuildHrEmailBody(verification.HrContactName, verification.EmployeeFullName, verification.CompanyName, verification.JobTitle, confirmationLink);

        await _emailSender.SendEmailAsync(verification.HrEmail, subject, body, cancellationToken);

        return true;
    }

    private static string BuildHrEmailBody(string? hrContactName, string employeeFullName, string companyName, string jobTitle, string confirmationLink)
    {
        var greeting = string.IsNullOrWhiteSpace(hrContactName) ? "Dear HR Representative" : $"Dear {hrContactName}";
        return $@"
<div style=""font-family:Arial,sans-serif;font-size:15px;color:#222;line-height:1.6;max-width:600px;margin:0 auto;padding:24px;border:1px solid #ddd;border-radius:6px;"">
  <h2 style=""color:#1a56db;"">Employment Verification Request</h2>
  <p>{greeting},</p>
  <p>We have received an employment verification request for:</p>
  <table style=""border-collapse:collapse;margin:16px 0;"">
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Employee:</td><td>{System.Net.WebUtility.HtmlEncode(employeeFullName)}</td></tr>
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Company:</td><td>{System.Net.WebUtility.HtmlEncode(companyName)}</td></tr>
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Job Title:</td><td>{System.Net.WebUtility.HtmlEncode(jobTitle)}</td></tr>
  </table>
  <p>Please click the button below to confirm or dispute this information:</p>
  <p style=""margin:24px 0;"">
    <a href=""{confirmationLink}"" style=""display:inline-block;padding:12px 24px;background-color:#1a56db;color:#fff;text-decoration:none;border-radius:4px;font-weight:bold;"">Respond to Verification</a>
  </p>
  <p><strong>This link will expire in 48 hours.</strong></p>
  <p>If you did not expect this request, please ignore this email or contact us immediately.</p>
  <div style=""margin-top:32px;font-size:12px;color:#666;border-top:1px solid #eee;padding-top:16px;"">
    <p>Employment Verify — POPIA-compliant employment verification platform.</p>
  </div>
</div>";
    }
}
