using System.Net;
using System.Text.Json;

namespace EmploymentVerify.Api.Middleware;

/// <summary>
/// Catches any unhandled exception and returns a consistent RFC 7807 ProblemDetails response.
/// Stack traces are never exposed outside Development.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemResponseAsync(context, ex);
        }
    }

    private async Task WriteProblemResponseAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = ex switch
        {
            InvalidOperationException ioe when ioe.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Forbidden,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = context.Response.StatusCode switch
            {
                404 => "Not Found",
                403 => "Forbidden",
                _ => "An unexpected error occurred"
            },
            status = context.Response.StatusCode,
            detail = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred. Please try again.",
            trace = _env.IsDevelopment() ? ex.StackTrace : null,
            correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
