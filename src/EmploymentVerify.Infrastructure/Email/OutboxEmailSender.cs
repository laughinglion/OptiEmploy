using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Infrastructure.Persistence;

namespace EmploymentVerify.Infrastructure.Email;

/// <summary>
/// Writes emails to the outbox table atomically with the calling operation.
/// A background service (<see cref="OutboxDispatcherService"/>) picks them up and sends via SMTP.
/// </summary>
public class OutboxEmailSender : IEmailSender
{
    private readonly ApplicationDbContext _context;

    public OutboxEmailSender(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            CreatedAt = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
