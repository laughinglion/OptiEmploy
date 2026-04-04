using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EmploymentVerify.Infrastructure.Authorization;

/// <summary>
/// Handles <see cref="RoleRequirement"/> by checking the user's role claim(s).
/// The role claims are expected to be stored as <see cref="ClaimTypes.Role"/>.
/// Supports users with multiple role claims (OR logic — any match suffices).
/// </summary>
public sealed class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            return Task.CompletedTask; // not authenticated — requirement not met
        }

        // Collect all role claims for the user (supports multiple role assignments)
        var userRoles = context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (userRoles.Count == 0)
        {
            return Task.CompletedTask; // no roles assigned — requirement not met
        }

        // Check if any of the user's roles match any of the allowed roles
        if (userRoles.Any(userRole =>
            requirement.AllowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
