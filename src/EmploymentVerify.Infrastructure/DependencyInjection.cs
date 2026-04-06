using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Infrastructure.Authentication;
using EmploymentVerify.Infrastructure.Email;
using EmploymentVerify.Infrastructure.Persistence;
using EmploymentVerify.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmploymentVerify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IEmailVerificationTokenGenerator, CryptoTokenGenerator>();
        services.AddSingleton<IFieldEncryption, AesEncryptionService>();

        // Expose JwtSettings as IRefreshTokenSettings for Application layer handlers
        services.AddSingleton<IRefreshTokenSettings>(sp =>
        {
            var settings = new JwtSettings();
            configuration.GetSection(JwtSettings.SectionName).Bind(settings);
            return settings;
        });

        // Email — outbox pattern: writes to DB; OutboxDispatcherService delivers via SMTP
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddTransient<SmtpEmailSender>(); // concrete type for OutboxDispatcherService
        services.AddScoped<IEmailSender, OutboxEmailSender>();

        // Pricing
        services.Configure<PricingSettings>(configuration.GetSection(PricingSettings.SectionName));

        // Background services
        services.AddHostedService<BackgroundServices.EmailRetryService>();
        services.AddHostedService<BackgroundServices.TokenExpiryService>();
        services.AddHostedService<BackgroundServices.ExpiredTokenCleanupService>();
        services.AddHostedService<BackgroundServices.OutboxDispatcherService>();

        return services;
    }
}
