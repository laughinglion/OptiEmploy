using System.Diagnostics;
using System.Security.Claims;

namespace EmploymentVerify.Api.Middleware;

/// <summary>
/// Middleware that logs API requests with user identity and role information
/// for POPIA audit compliance. Logs the user ID, role, endpoint, and response status.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value ?? "none";
        var method = context.Request.Method;
        var path = context.Request.Path;
        var statusCode = context.Response.StatusCode;

        _logger.LogInformation(
            "API {Method} {Path} -> {StatusCode} | User: {UserId} | Role: {UserRole} | Duration: {Duration}ms",
            method, path, statusCode, userId, userRole, stopwatch.ElapsedMilliseconds);
    }
}
