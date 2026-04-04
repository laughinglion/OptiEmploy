using Microsoft.AspNetCore.Authorization;

namespace EmploymentVerify.Infrastructure.Authorization;

/// <summary>
/// An authorization requirement that demands the user possess one of the specified roles.
/// </summary>
public sealed class RoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// One or more roles that satisfy this requirement (OR logic — any one suffices).
    /// </summary>
    public IReadOnlyList<string> AllowedRoles { get; }

    public RoleRequirement(params string[] allowedRoles)
    {
        if (allowedRoles is null || allowedRoles.Length == 0)
            throw new ArgumentException("At least one role must be specified.", nameof(allowedRoles));

        AllowedRoles = allowedRoles;
    }
}
