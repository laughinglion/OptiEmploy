using EmploymentVerify.Api.Endpoints;
using EmploymentVerify.Api.Middleware;
using EmploymentVerify.Application;
using EmploymentVerify.Application.Common;
using EmploymentVerify.Infrastructure;
using EmploymentVerify.Infrastructure.Authentication;
using EmploymentVerify.Infrastructure.Authorization;
using EmploymentVerify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Formatting.Compact;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console(new CompactJsonFormatter()));

// CORS — allow Web app to call API
var webOrigin = builder.Configuration.GetValue<string>("WebOrigin") ?? "https://localhost:5002";
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebApp", policy =>
    {
        policy.WithOrigins(webOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add services
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT authentication and role-based authorization
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddAppAuthorization();

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// CORS — before auth
app.UseCors("WebApp");

// Rate limiting middleware — before authentication
app.UseRateLimiter();

// Authentication & authorization middleware — order matters
app.UseAuthentication();
app.UseAuthorization();

// POPIA audit logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Map endpoints
app.MapAuthEndpoints();
app.MapCompanyEndpoints();
app.MapCompanySearchEndpoints();
app.MapVerificationEndpoints();

// Health check endpoint
app.MapHealthChecks("/health");

// Auto-migrate on startup (all environments)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Seed default admin user if none exists
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
    await DatabaseSeeder.SeedAsync(db, passwordHasher, app.Configuration, seederLogger);
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
