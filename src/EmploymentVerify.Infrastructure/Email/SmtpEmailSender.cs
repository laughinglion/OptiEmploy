using System.Net;
using System.Net.Mail;
using EmploymentVerify.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Infrastructure.Email;

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@employmentverify.co.za";
    public string FromName { get; set; } = "Employment Verify";
    public bool EnableSsl { get; set; } = true;
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.EnableSsl
        };

        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(to));

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Verification email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", to);
            throw;
        }
    }
}
