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

// Health checks — liveness (always healthy) and readiness (checks DB)
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, tags: ["ready"]);

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
    options.AddFixedWindowLimiter("verification", opt =>
    {
        opt.PermitLimit = 20;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("admin", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Validate critical secrets in non-Development environments ──
if (!builder.Environment.IsDevelopment())
{
    var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "";
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.StartsWith("CHANGE_THIS"))
        throw new InvalidOperationException("Jwt:SecretKey must be set to a secure value in production. Set via environment variable Jwt__SecretKey.");

    var encryptionKey = builder.Configuration["Encryption:FieldKey"] ?? "";
    if (string.IsNullOrWhiteSpace(encryptionKey) || encryptionKey.StartsWith("CHANGE_THIS"))
        throw new InvalidOperationException("Encryption:FieldKey must be set to a secure value in production. Set via environment variable Encryption__FieldKey.");

    var smtpPassword = builder.Configuration["Smtp:Password"] ?? "";
    var smtpUsername = builder.Configuration["Smtp:Username"] ?? "";
    if (string.IsNullOrWhiteSpace(smtpPassword) || smtpPassword == "your-app-password-here"
        || string.IsNullOrWhiteSpace(smtpUsername) || smtpUsername == "your-email@gmail.com")
        throw new InvalidOperationException("Smtp:Username and Smtp:Password must be configured for production. Set via environment variables.");

    var allowedHosts = builder.Configuration["AllowedHosts"] ?? "*";
    if (allowedHosts == "*")
        Log.Warning("AllowedHosts is set to '*' — this allows Host header injection. Set AllowedHosts to your domain in production.");
}

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Correlation ID + global exception handler — outermost middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

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
app.MapMeEndpoints();
app.MapCompanyEndpoints();
app.MapCompanySearchEndpoints();
app.MapVerificationEndpoints();

// Health check endpoints — /health/live for liveness, /health/ready for readiness (checks DB)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // no checks — always returns Healthy
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health"); // backwards-compatible: runs all checks

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
