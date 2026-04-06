using Serilog.Context;

namespace EmploymentVerify.Api.Middleware;

/// <summary>
/// Reads the X-Correlation-ID request header (or generates a new one) and:
/// - Echos it back in the response header so callers can trace requests end-to-end
/// - Pushes it into Serilog's LogContext so every log line for this request includes it
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
