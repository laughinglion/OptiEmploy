using System.Security.Claims;

namespace EmploymentVerify.Api.Filters;

/// <summary>
/// Minimal API endpoint filter that enforces role-based authorization.
/// Returns 403 Forbidden if the authenticated user's role is not in the allowed set.
/// Usage: <c>.AddEndpointFilter(new RoleAuthorizationFilter("Admin"))</c>
/// </summary>
public sealed class RoleAuthorizationFilter : IEndpointFilter
{
    private readonly string[] _allowedRoles;

    public RoleAuthorizationFilter(params string[] allowedRoles)
    {
        if (allowedRoles.Length == 0)
            throw new ArgumentException("At least one role must be specified.", nameof(allowedRoles));

        _allowedRoles = allowedRoles;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return Results.Json(
                new { error = "unauthorized", message = "Authentication is required." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole is null || !_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            return Results.Json(
                new { error = "forbidden", message = "You do not have permission to access this resource." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
