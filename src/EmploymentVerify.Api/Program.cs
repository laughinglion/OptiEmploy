using EmploymentVerify.Api.Endpoints;
using EmploymentVerify.Api.Middleware;
using EmploymentVerify.Application;
using EmploymentVerify.Infrastructure;
using EmploymentVerify.Infrastructure.Authentication;
using EmploymentVerify.Infrastructure.Authorization;
using EmploymentVerify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add JWT authentication and role-based authorization
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddAppAuthorization();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

// Auto-migrate in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
