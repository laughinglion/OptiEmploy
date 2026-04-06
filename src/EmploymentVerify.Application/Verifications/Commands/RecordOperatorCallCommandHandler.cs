using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Verifications.Commands;

public class RecordOperatorCallCommandHandler : IRequestHandler<RecordOperatorCallCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;

    public RecordOperatorCallCommandHandler(IApplicationDbContext context, IEmailSender emailSender)
    {
        _context = context;
        _emailSender = emailSender;
    }

    public async Task<bool> Handle(RecordOperatorCallCommand request, CancellationToken cancellationToken)
    {
        var verification = await _context.VerificationRequests
            .FirstOrDefaultAsync(v => v.Id == request.VerificationRequestId, cancellationToken);

        if (verification is null)
            return false;

        var note = new OperatorNote
        {
            Id = Guid.NewGuid(),
            VerificationRequestId = request.VerificationRequestId,
            OperatorId = request.OperatorId,
            CallOutcome = request.CallOutcome,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.OperatorNotes.Add(note);

        var isTerminal = false;
        switch (request.CallOutcome)
        {
            case CallOutcome.Confirmed:
                verification.Status = VerificationStatus.Confirmed;
                verification.VerificationMethod = VerificationMethod.Phone;
                verification.CompletedAt = DateTime.UtcNow;
                isTerminal = true;

                var response = new VerificationResponse
                {
                    Id = Guid.NewGuid(),
                    VerificationRequestId = request.VerificationRequestId,
                    RespondedBy = request.OperatorId.ToString(),
                    ResponseType = ResponseType.Confirmed,
                    ConfirmedJobTitle = request.ConfirmedJobTitle,
                    ConfirmedStartDate = request.ConfirmedStartDate,
                    ConfirmedEndDate = request.ConfirmedEndDate,
                    IsCurrentlyEmployed = request.IsCurrentlyEmployed,
                    Notes = request.ResponseNotes,
                    RespondedAt = DateTime.UtcNow
                };
                _context.VerificationResponses.Add(response);
                break;

            case CallOutcome.Denied:
                verification.Status = VerificationStatus.Denied;
                verification.VerificationMethod = VerificationMethod.Phone;
                verification.CompletedAt = DateTime.UtcNow;
                isTerminal = true;
                break;

            case CallOutcome.Unreachable:
            case CallOutcome.CallbackScheduled:
                verification.Status = VerificationStatus.InProgress;
                break;
        }

        verification.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        if (isTerminal)
            await NotifyRequestorAsync(verification, cancellationToken);

        return true;
    }

    private async Task NotifyRequestorAsync(VerificationRequest verification, CancellationToken cancellationToken)
    {
        try
        {
            var requestor = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == verification.RequestorId, cancellationToken);

            if (requestor is null) return;

            var statusLabel = verification.Status == VerificationStatus.Confirmed ? "Confirmed ✓" : "Denied";
            var subject = $"Verification Update: {verification.EmployeeFullName}";
            var body = $@"
<div style=""font-family:Arial,sans-serif;font-size:15px;color:#222;max-width:600px;margin:0 auto;padding:24px;border:1px solid #ddd;border-radius:6px;"">
  <h2 style=""color:#1a56db;"">Verification Update</h2>
  <p>Hi {System.Net.WebUtility.HtmlEncode(requestor.FullName)},</p>
  <p>The employment verification for <strong>{System.Net.WebUtility.HtmlEncode(verification.EmployeeFullName)}</strong>
     at <strong>{System.Net.WebUtility.HtmlEncode(verification.CompanyName)}</strong> has been completed.</p>
  <table style=""border-collapse:collapse;margin:16px 0;"">
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Status:</td><td>{statusLabel}</td></tr>
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Method:</td><td>Phone Call (Operator)</td></tr>
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Reference:</td><td style=""font-family:monospace;font-size:12px;"">{verification.Id}</td></tr>
  </table>
  <p>Log in to your dashboard to view the full details.</p>
  <div style=""margin-top:32px;font-size:12px;color:#666;border-top:1px solid #eee;padding-top:16px;"">
    <p>Employment Verify — POPIA-compliant employment verification.</p>
  </div>
</div>";

            await _emailSender.SendEmailAsync(requestor.Email, subject, body, cancellationToken);
        }
        catch
        {
            // Notification failure must never roll back the call result
        }
    }
}
