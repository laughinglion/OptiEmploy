using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Infrastructure.Authentication;
using EmploymentVerify.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Tests.Api;

/// <summary>
/// Tests that the API authentication and authorization services are properly configured.
/// </summary>
public class ApiAuthorizationMiddlewareTests
{
    [Fact]
    public void AddAppAuthorization_RegistersAllPolicies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        foreach (var policyName in AuthorizationPolicies.All)
        {
            var policy = options.GetPolicy(policyName);
            Assert.NotNull(policy);
        }
    }

    [Fact]
    public void AddAppAuthorization_FallbackPolicyRequiresAuthentication()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.FallbackPolicy);
    }

    [Fact]
    public void AddAppAuthorization_RegistersRoleRequirementHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<IAuthorizationHandler>().ToList();

        Assert.Contains(handlers, h => h is RoleRequirementHandler);
    }

    [Fact]
    public void AdminPolicy_RequiresAdminRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        var policy = options.GetPolicy(AuthorizationPolicies.RequireAdmin)!;
        var roleReq = policy.Requirements.OfType<RoleRequirement>().Single();

        Assert.Contains(AppRoles.Admin, roleReq.AllowedRoles);
        Assert.Single(roleReq.AllowedRoles);
    }

    [Fact]
    public void AdminOrOperatorPolicy_AllowsBothRoles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        var policy = options.GetPolicy(AuthorizationPolicies.RequireAdminOrOperator)!;
        var roleReq = policy.Requirements.OfType<RoleRequirement>().Single();

        Assert.Contains(AppRoles.Admin, roleReq.AllowedRoles);
        Assert.Contains(AppRoles.Operator, roleReq.AllowedRoles);
        Assert.Equal(2, roleReq.AllowedRoles.Count);
    }

    [Fact]
    public void AdminOrRequestorPolicy_AllowsBothRoles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        var policy = options.GetPolicy(AuthorizationPolicies.RequireAdminOrRequestor)!;
        var roleReq = policy.Requirements.OfType<RoleRequirement>().Single();

        Assert.Contains(AppRoles.Admin, roleReq.AllowedRoles);
        Assert.Contains(AppRoles.Requestor, roleReq.AllowedRoles);
        Assert.Equal(2, roleReq.AllowedRoles.Count);
    }

    [Fact]
    public void AnyRolePolicy_AllowsAllThreeRoles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        var policy = options.GetPolicy(AuthorizationPolicies.RequireAnyRole)!;
        var roleReq = policy.Requirements.OfType<RoleRequirement>().Single();

        Assert.Contains(AppRoles.Admin, roleReq.AllowedRoles);
        Assert.Contains(AppRoles.Requestor, roleReq.AllowedRoles);
        Assert.Contains(AppRoles.Operator, roleReq.AllowedRoles);
        Assert.Equal(3, roleReq.AllowedRoles.Count);
    }

    [Fact]
    public void JwtSettings_HasRequiredDefaults()
    {
        var settings = new JwtSettings();

        Assert.Equal("EmploymentVerify", settings.Issuer);
        Assert.Equal("EmploymentVerify", settings.Audience);
        Assert.Equal(60, settings.ExpirationMinutes);
        Assert.Equal(7, settings.RefreshTokenExpirationDays);
    }
}
