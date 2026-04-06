using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmploymentVerify.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var adminEmail = configuration["AdminSeed:Email"];
        var adminPassword = configuration["AdminSeed:Password"];
        var adminName = configuration["AdminSeed:FullName"] ?? "System Administrator";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("AdminSeed:Email or AdminSeed:Password not configured — skipping admin seed");
            return;
        }

        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        var adminExists = await context.Users.AnyAsync(u => u.Role == UserRole.Admin);

        if (adminExists)
        {
            logger.LogInformation("Admin user already exists — skipping seed");
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            FullName = adminName,
            Role = UserRole.Admin,
            CreditBalance = 0m,
            IsActive = true,
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded initial admin user: {Email}", normalizedEmail);
    }
}
