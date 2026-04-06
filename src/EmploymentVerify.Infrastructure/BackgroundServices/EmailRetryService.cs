using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmploymentVerify.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that retries sending verification emails for Pending requests
/// where the company is in the directory, is not force-call, has an HR email,
/// and no valid (non-expired, non-used) token exists yet.
/// Runs every 5 minutes.
/// </summary>
public class EmailRetryService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailRetryService> _logger;
    private readonly string _baseUrl;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public EmailRetryService(IServiceProvider services, ILogger<EmailRetryService> logger, IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:5001";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await RetryPendingEmailsAsync(stoppingToken);
        }
    }

    private async Task RetryPendingEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            // Find Pending requests that should have had an email sent:
            // - Company is in directory (CompanyId not null)
            // - Company is not force-call
            // - Has an HR email
            // - No valid (unused, non-expired) token exists
            var now = DateTime.UtcNow;

            var pendingIds = await context.VerificationRequests
                .Where(v =>
                    v.Status == Domain.Enums.VerificationStatus.Pending &&
                    v.CompanyId != null &&
                    v.HrEmail != null &&
                    v.CreatedAt < now.AddMinutes(-5)) // Give initial attempt time to complete
                .Join(context.Companies,
                    v => v.CompanyId,
                    c => c.Id,
                    (v, c) => new { Verification = v, Company = c })
                .Where(x => !x.Company.ForceCall && x.Company.IsActive)
                .Select(x => x.Verification.Id)
                .ToListAsync(cancellationToken);

            // Filter out those that already have a valid token
            var alreadySentIds = await context.EmailVerificationTokens
                .Where(t => pendingIds.Contains(t.VerificationRequestId) && !t.IsUsed && t.ExpiresAt > now)
                .Select(t => t.VerificationRequestId)
                .ToListAsync(cancellationToken);

            var toRetry = pendingIds.Except(alreadySentIds).ToList();

            if (toRetry.Count == 0) return;

            _logger.LogInformation("Email retry: found {Count} pending verifications to retry", toRetry.Count);

            foreach (var id in toRetry)
            {
                try
                {
                    await mediator.Send(new SendVerificationEmailCommand(id, _baseUrl), cancellationToken);
                    _logger.LogInformation("Email retry: sent email for verification {Id}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email retry: failed to send email for verification {Id}", id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email retry service encountered an error");
        }
    }
}
