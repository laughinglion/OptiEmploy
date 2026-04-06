using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmploymentVerify.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that moves EmailSent/InProgress verifications with expired tokens
/// to UnableToVerify, and routes them to the operator queue.
/// Runs every 30 minutes.
/// </summary>
public class TokenExpiryService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenExpiryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public TokenExpiryService(IServiceProvider services, ILogger<TokenExpiryService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await ExpireTokensAsync(stoppingToken);
        }
    }

    private async Task ExpireTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var now = DateTime.UtcNow;

            // Find InProgress verifications with all tokens expired/used
            var expiredVerificationIds = await context.EmailVerificationTokens
                .Where(t => t.ExpiresAt < now && !t.IsUsed)
                .Select(t => t.VerificationRequestId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (expiredVerificationIds.Count == 0) return;

            var updated = await context.VerificationRequests
                .Where(v =>
                    expiredVerificationIds.Contains(v.Id) &&
                    v.Status == VerificationStatus.InProgress)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.Status, VerificationStatus.Pending) // Back to Pending so operator can pick it up
                    .SetProperty(v => v.UpdatedAt, now),
                    cancellationToken);

            if (updated > 0)
                _logger.LogInformation("TokenExpiry: returned {Count} expired email verifications to operator queue", updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token expiry service encountered an error");
        }
    }
}
