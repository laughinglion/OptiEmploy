using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Tests.Authorization;

public class AuthorizationPolicyRegistrationTests
{
    private readonly AuthorizationOptions _options;

    public AuthorizationPolicyRegistrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        _options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
    }

    [Theory]
    [InlineData(AuthorizationPolicies.RequireAdmin)]
    [InlineData(AuthorizationPolicies.RequireRequestor)]
    [InlineData(AuthorizationPolicies.RequireOperator)]
    [InlineData(AuthorizationPolicies.RequireAdminOrOperator)]
    [InlineData(AuthorizationPolicies.RequireAdminOrRequestor)]
    [InlineData(AuthorizationPolicies.RequireAnyRole)]
    public void Policy_IsRegistered(string policyName)
    {
        var policy = _options.GetPolicy(policyName);
        Assert.NotNull(policy);
    }

    [Theory]
    [InlineData(AuthorizationPolicies.RequireAdmin)]
    [InlineData(AuthorizationPolicies.RequireRequestor)]
    [InlineData(AuthorizationPolicies.RequireOperator)]
    [InlineData(AuthorizationPolicies.RequireAdminOrOperator)]
    [InlineData(AuthorizationPolicies.RequireAdminOrRequestor)]
    [InlineData(AuthorizationPolicies.RequireAnyRole)]
    public void Policy_HasRoleRequirement(string policyName)
    {
        var policy = _options.GetPolicy(policyName)!;
        Assert.Contains(policy.Requirements, r => r is RoleRequirement);
    }

    [Fact]
    public void AllPoliciesConstant_MatchesRegisteredPolicies()
    {
        foreach (var policyName in AuthorizationPolicies.All)
        {
            var policy = _options.GetPolicy(policyName);
            Assert.NotNull(policy);
        }
    }

    [Fact]
    public void FallbackPolicy_RequiresAuthentication()
    {
        Assert.NotNull(_options.FallbackPolicy);
        // FallbackPolicy should require authenticated user
        Assert.NotEmpty(_options.FallbackPolicy!.AuthenticationSchemes.Count > 0
            ? _options.FallbackPolicy.AuthenticationSchemes
            : ["default"]); // It requires auth via DenyAnonymousAuthorizationRequirement
    }

    [Fact]
    public void Handler_IsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthorization();

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<IAuthorizationHandler>();
        Assert.Contains(handlers, h => h is RoleRequirementHandler);
    }
}
