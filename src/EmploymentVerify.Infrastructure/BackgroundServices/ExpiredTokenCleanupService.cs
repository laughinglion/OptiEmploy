using EmploymentVerify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmploymentVerify.Infrastructure.BackgroundServices;

/// <summary>
/// Nightly job that deletes expired/revoked refresh tokens and used/expired email verification tokens.
/// </summary>
public class ExpiredTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiredTokenCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public ExpiredTokenCleanupService(IServiceProvider services, ILogger<ExpiredTokenCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay initial run by 1 minute so startup isn't impacted
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            var now = DateTime.UtcNow;

            var deletedRefresh = await context.RefreshTokens
                .Where(t => t.IsRevoked || t.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedEmailTokens = await context.EmailVerificationTokens
                .Where(t => t.IsUsed || t.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedRefresh > 0 || deletedEmailTokens > 0)
            {
                _logger.LogInformation(
                    "Token cleanup: deleted {RefreshCount} expired/revoked refresh tokens and {EmailCount} used/expired email tokens",
                    deletedRefresh, deletedEmailTokens);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token cleanup service encountered an error");
        }
    }
}
