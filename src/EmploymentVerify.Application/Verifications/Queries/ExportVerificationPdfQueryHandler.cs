using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EmploymentVerify.Application.Verifications.Queries;

/// <summary>
/// Generates a simple HTML-based PDF certificate for a completed verification.
/// Returns null if the verification is not found, not accessible, or not yet completed.
/// </summary>
public class ExportVerificationPdfQueryHandler : IRequestHandler<ExportVerificationPdfQuery, byte[]?>
{
    private readonly IApplicationDbContext _context;

    public ExportVerificationPdfQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]?> Handle(ExportVerificationPdfQuery request, CancellationToken cancellationToken)
    {
        var verification = await _context.VerificationRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VerificationId, cancellationToken);

        if (verification is null) return null;
        if (!request.IsAdmin && verification.RequestorId != request.RequestorId) return null;
        if (verification.Status != VerificationStatus.Confirmed &&
            verification.Status != VerificationStatus.UnableToVerify) return null;

        var response = await _context.VerificationResponses
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.VerificationRequestId == request.VerificationId, cancellationToken);

        var html = BuildCertificateHtml(verification, response);
        return Encoding.UTF8.GetBytes(html);
    }

    private static string BuildCertificateHtml(
        Domain.Entities.VerificationRequest v,
        Domain.Entities.VerificationResponse? r)
    {
        var statusColor = v.Status == VerificationStatus.Confirmed ? "#16a34a" : "#dc2626";
        var statusLabel = v.Status == VerificationStatus.Confirmed ? "VERIFIED" : "UNABLE TO VERIFY";
        var endDate = v.EmploymentEndDate.HasValue ? v.EmploymentEndDate.Value.ToString("dd MMM yyyy") : "Present";

        var responseSection = r is not null ? $@"
<div class='section'>
  <h3>HR Confirmation</h3>
  <table>
    <tr><td>Responded By</td><td>{Encode(r.RespondedBy)}</td></tr>
    <tr><td>Response</td><td>{r.ResponseType}</td></tr>
    {(r.ConfirmedJobTitle is not null ? $"<tr><td>Confirmed Job Title</td><td>{Encode(r.ConfirmedJobTitle)}</td></tr>" : "")}
    {(r.ConfirmedStartDate.HasValue ? $"<tr><td>Confirmed Start Date</td><td>{r.ConfirmedStartDate.Value:dd MMM yyyy}</td></tr>" : "")}
    {(r.ConfirmedEndDate.HasValue ? $"<tr><td>Confirmed End Date</td><td>{r.ConfirmedEndDate.Value:dd MMM yyyy}</td></tr>" : "")}
    {(r.IsCurrentlyEmployed.HasValue ? $"<tr><td>Currently Employed</td><td>{(r.IsCurrentlyEmployed.Value ? "Yes" : "No")}</td></tr>" : "")}
    <tr><td>Date Responded</td><td>{r.RespondedAt:dd MMM yyyy HH:mm} UTC</td></tr>
  </table>
</div>" : "<p><em>No HR response recorded.</em></p>";

        return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<title>Employment Verification Certificate</title>
<style>
  body {{ font-family: Arial, sans-serif; font-size: 13px; color: #222; margin: 40px; }}
  h1 {{ color: #1a56db; border-bottom: 2px solid #1a56db; padding-bottom: 8px; }}
  h3 {{ color: #444; margin-top: 24px; }}
  .badge {{ display: inline-block; padding: 6px 18px; border-radius: 4px; font-weight: bold; font-size: 16px; color: #fff; background-color: {statusColor}; }}
  .section {{ margin: 20px 0; padding: 16px; border: 1px solid #ddd; border-radius: 4px; }}
  table {{ width: 100%; border-collapse: collapse; }}
  td {{ padding: 6px 12px 6px 0; vertical-align: top; }}
  td:first-child {{ font-weight: bold; width: 40%; color: #555; }}
  .footer {{ margin-top: 40px; font-size: 11px; color: #888; border-top: 1px solid #eee; padding-top: 12px; }}
  @media print {{ body {{ margin: 20px; }} }}
</style>
</head>
<body>
<h1>Employment Verification Certificate</h1>
<p><span class='badge'>{statusLabel}</span></p>
<div class='section'>
  <h3>Employee Details</h3>
  <table>
    <tr><td>Full Name</td><td>{Encode(v.EmployeeFullName)}</td></tr>
    <tr><td>Job Title</td><td>{Encode(v.JobTitle)}</td></tr>
    <tr><td>Employment Start</td><td>{v.EmploymentStartDate:dd MMM yyyy}</td></tr>
    <tr><td>Employment End</td><td>{endDate}</td></tr>
  </table>
</div>
<div class='section'>
  <h3>Company Details</h3>
  <table>
    <tr><td>Company Name</td><td>{Encode(v.CompanyName)}</td></tr>
  </table>
</div>
{responseSection}
<div class='section'>
  <h3>Request Details</h3>
  <table>
    <tr><td>Reference ID</td><td>{v.Id}</td></tr>
    <tr><td>Submitted</td><td>{v.CreatedAt:dd MMM yyyy HH:mm} UTC</td></tr>
    {(v.CompletedAt.HasValue ? $"<tr><td>Completed</td><td>{v.CompletedAt.Value:dd MMM yyyy HH:mm} UTC</td></tr>" : "")}
    <tr><td>POPIA Consent</td><td>{(v.PopiaConsentGiven ? "Given" : "Not given")} ({v.ConsentType})</td></tr>
  </table>
</div>
<div class='footer'>
  <p>This certificate was generated by Employment Verify, a POPIA-compliant employment verification platform.
  Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC</p>
</div>
</body>
</html>";
    }

    private static string Encode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
