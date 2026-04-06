using EmploymentVerify.Infrastructure.Email;
using EmploymentVerify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Infrastructure.BackgroundServices;

/// <summary>
/// Polls the outbox_messages table every 30 seconds and dispatches unsent emails via SMTP.
/// Retries up to 5 times before giving up.
/// </summary>
public class OutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxDispatcherService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private const int MaxAttempts = 5;

    public OutboxDispatcherService(IServiceProvider services, ILogger<OutboxDispatcherService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await DispatchPendingAsync(stoppingToken);
        }
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var smtpSender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();

        try
        {
            var pending = await context.OutboxMessages
                .Where(m => m.SentAt == null && m.AttemptCount < MaxAttempts)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            if (pending.Count == 0)
            {
                // Check for dead-lettered messages (exhausted retries) and log critical alerts
                var deadLetterCount = await context.OutboxMessages
                    .CountAsync(m => m.SentAt == null && m.AttemptCount >= MaxAttempts, cancellationToken);

                if (deadLetterCount > 0)
                    _logger.LogCritical("Outbox: {Count} email(s) have exhausted all {Max} retry attempts and will not be sent. Manual intervention required.", deadLetterCount, MaxAttempts);

                return;
            }

            foreach (var msg in pending)
            {
                msg.AttemptCount++;
                try
                {
                    await smtpSender.SendEmailAsync(msg.To, msg.Subject, msg.HtmlBody, cancellationToken);
                    msg.SentAt = DateTime.UtcNow;
                    _logger.LogInformation("Outbox: sent email {Id} to {To}", msg.Id, msg.To);
                }
                catch (Exception ex)
                {
                    msg.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                    _logger.LogWarning(ex, "Outbox: failed attempt {Attempt}/{Max} for email {Id}", msg.AttemptCount, MaxAttempts, msg.Id);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Outbox dispatcher encountered an error");
        }
    }
}
