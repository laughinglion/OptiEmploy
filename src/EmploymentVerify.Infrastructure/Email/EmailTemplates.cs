namespace EmploymentVerify.Infrastructure.Email;

/// <summary>
/// Centralised HTML email templates. All inline styles for maximum email client compatibility.
/// </summary>
public static class EmailTemplates
{
    private const string BaseStyle = @"
        font-family: Arial, sans-serif;
        font-size: 15px;
        color: #222;
        line-height: 1.6;
        max-width: 600px;
        margin: 0 auto;
        padding: 24px;
        border: 1px solid #ddd;
        border-radius: 6px;";

    private const string ButtonStyle = @"
        display: inline-block;
        padding: 12px 24px;
        background-color: #1a56db;
        color: #fff;
        text-decoration: none;
        border-radius: 4px;
        font-weight: bold;";

    private const string FooterStyle = @"
        margin-top: 32px;
        font-size: 12px;
        color: #666;
        border-top: 1px solid #eee;
        padding-top: 16px;";

    /// <summary>HR email requesting employment confirmation.</summary>
    public static string HrVerificationRequest(
        string hrContactName,
        string employeeFullName,
        string companyName,
        string jobTitle,
        string confirmationLink)
    {
        var greeting = string.IsNullOrWhiteSpace(hrContactName) ? "Dear HR Representative" : $"Dear {hrContactName}";
        return $@"
<div style=""{BaseStyle}"">
  <h2 style=""color:#1a56db;"">Employment Verification Request</h2>
  <p>{greeting},</p>
  <p>We have received an employment verification request for:</p>
  <table style=""border-collapse:collapse;margin:16px 0;"">
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Employee:</td><td>{HtmlEncode(employeeFullName)}</td></tr>
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Company:</td><td>{HtmlEncode(companyName)}</td></tr>
    <tr><td style=""padding:4px 16px 4px 0;font-weight:bold;"">Job Title:</td><td>{HtmlEncode(jobTitle)}</td></tr>
  </table>
  <p>Please click the button below to confirm or dispute this information:</p>
  <p style=""margin:24px 0;"">
    <a href=""{confirmationLink}"" style=""{ButtonStyle}"">Respond to Verification</a>
  </p>
  <p>Or copy this link into your browser:<br/>
    <small style=""color:#555;word-break:break-all;"">{confirmationLink}</small>
  </p>
  <p><strong>This link will expire in 48 hours.</strong></p>
  <p>If you did not expect this request or have concerns, please ignore this email or contact us immediately.</p>
  <div style=""{FooterStyle}"">
    <p>This message was sent by Employment Verify — a POPIA-compliant employment verification platform.</p>
  </div>
</div>";
    }

    /// <summary>Email sent to new user with address verification link.</summary>
    public static string EmailVerification(string fullName, string confirmationLink)
    {
        return $@"
<div style=""{BaseStyle}"">
  <h2 style=""color:#1a56db;"">Verify Your Email Address</h2>
  <p>Hi {HtmlEncode(fullName)},</p>
  <p>Thank you for registering with Employment Verify. Please verify your email address to activate your account.</p>
  <p style=""margin:24px 0;"">
    <a href=""{confirmationLink}"" style=""{ButtonStyle}"">Verify Email</a>
  </p>
  <p>Or copy this link into your browser:<br/>
    <small style=""color:#555;word-break:break-all;"">{confirmationLink}</small>
  </p>
  <p>This link expires in 24 hours. If you did not create an account, you can safely ignore this email.</p>
  <div style=""{FooterStyle}"">
    <p>Employment Verify — POPIA-compliant employment verification.</p>
  </div>
</div>";
    }

    /// <summary>Notifies requestor that HR has responded to their verification.</summary>
    public static string VerificationCompleted(
        string requestorName,
        string employeeFullName,
        string status,
        string dashboardLink)
    {
        return $@"
<div style=""{BaseStyle}"">
  <h2 style=""color:#1a56db;"">Verification Update</h2>
  <p>Hi {HtmlEncode(requestorName)},</p>
  <p>The employment verification for <strong>{HtmlEncode(employeeFullName)}</strong> has been updated.</p>
  <p><strong>Status:</strong> {HtmlEncode(status)}</p>
  <p style=""margin:24px 0;"">
    <a href=""{dashboardLink}"" style=""{ButtonStyle}"">View in Dashboard</a>
  </p>
  <div style=""{FooterStyle}"">
    <p>Employment Verify — POPIA-compliant employment verification.</p>
  </div>
</div>";
    }

    private static string HtmlEncode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
