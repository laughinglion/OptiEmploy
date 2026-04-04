namespace EmploymentVerify.Application.Common;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
