using EmploymentVerify.Application.Common;
using EmploymentVerify.Infrastructure.Email;
using EmploymentVerify.Infrastructure.Persistence;
using EmploymentVerify.Infrastructure.Security;
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

        // Email
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
