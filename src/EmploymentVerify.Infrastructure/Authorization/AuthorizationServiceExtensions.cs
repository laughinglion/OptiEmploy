using EmploymentVerify.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EmploymentVerify.Infrastructure.Authorization;

/// <summary>
/// Extension methods to register role-based authorization policies and handlers.
/// </summary>
public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Registers all application authorization policies and the custom role requirement handler.
    /// Call this in Program.cs: <c>builder.Services.AddAppAuthorization();</c>
    /// </summary>
    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationCore(options =>
        {
            // Single-role policies
            options.AddPolicy(AuthorizationPolicies.RequireAdmin,
                policy => policy.Requirements.Add(new RoleRequirement(AppRoles.Admin)));

            options.AddPolicy(AuthorizationPolicies.RequireRequestor,
                policy => policy.Requirements.Add(new RoleRequirement(AppRoles.Requestor)));

            options.AddPolicy(AuthorizationPolicies.RequireOperator,
                policy => policy.Requirements.Add(new RoleRequirement(AppRoles.Operator)));

            // Composite policies
            options.AddPolicy(AuthorizationPolicies.RequireAdminOrOperator,
                policy => policy.Requirements.Add(new RoleRequirement(AppRoles.Admin, AppRoles.Operator)));

            options.AddPolicy(AuthorizationPolicies.RequireAdminOrRequestor,
                policy => policy.Requirements.Add(new RoleRequirement(AppRoles.Admin, AppRoles.Requestor)));

            // Any valid role — for shared pages accessible to all authenticated role-holders
            options.AddPolicy(AuthorizationPolicies.RequireAnyRole,
                policy => policy.Requirements.Add(
                    new RoleRequirement(AppRoles.Admin, AppRoles.Requestor, AppRoles.Operator)));

            // Default: require authentication for all pages unless explicitly marked [AllowAnonymous]
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddSingleton<IAuthorizationHandler, RoleRequirementHandler>();

        return services;
    }
}
