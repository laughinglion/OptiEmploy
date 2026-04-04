using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EmploymentVerify.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (migrations) to create ApplicationDbContext
/// without requiring the full application startup (which needs JWT settings, etc.).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../EmploymentVerify.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"),
            npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
