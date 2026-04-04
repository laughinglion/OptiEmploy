using System.Security.Claims;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace EmploymentVerify.Tests.Authorization;

public class RoleRequirementHandlerTests
{
    private readonly RoleRequirementHandler _handler = new();

    private static AuthorizationHandlerContext CreateContext(
        RoleRequirement requirement,
        ClaimsPrincipal user)
    {
        return new AuthorizationHandlerContext(
            [requirement],
            user,
            resource: null);
    }

    private static ClaimsPrincipal CreateUser(string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test@example.com")
        };

        if (role is not null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateAnonymousUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity()); // no authenticationType = unauthenticated
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.Requestor)]
    [InlineData(AppRoles.Operator)]
    public async Task Handler_Succeeds_WhenUserHasMatchingRole(string role)
    {
        var requirement = new RoleRequirement(role);
        var user = CreateUser(role);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Fails_WhenUserHasDifferentRole()
    {
        var requirement = new RoleRequirement(AppRoles.Admin);
        var user = CreateUser(AppRoles.Requestor);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Fails_WhenUserIsAnonymous()
    {
        var requirement = new RoleRequirement(AppRoles.Admin);
        var user = CreateAnonymousUser();
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Fails_WhenUserHasNoRoleClaim()
    {
        var requirement = new RoleRequirement(AppRoles.Admin);
        var user = CreateUser(role: null);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.Operator)]
    public async Task Handler_Succeeds_WithCompositeRequirement_WhenUserHasAnyAllowedRole(string role)
    {
        var requirement = new RoleRequirement(AppRoles.Admin, AppRoles.Operator);
        var user = CreateUser(role);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Fails_WithCompositeRequirement_WhenUserRoleNotInAllowed()
    {
        var requirement = new RoleRequirement(AppRoles.Admin, AppRoles.Operator);
        var user = CreateUser(AppRoles.Requestor);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void RoleRequirement_ThrowsIfNoRolesSpecified()
    {
        Assert.Throws<ArgumentException>(() => new RoleRequirement());
    }

    [Fact]
    public async Task Handler_IsCaseInsensitive()
    {
        var requirement = new RoleRequirement("admin");
        var user = CreateUser("Admin");
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Succeeds_WhenUserHasMultipleRoleClaims_AndOneMatches()
    {
        var requirement = new RoleRequirement(AppRoles.Operator);
        var user = CreateUserWithMultipleRoles(AppRoles.Requestor, AppRoles.Operator);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Fails_WhenUserHasMultipleRoleClaims_NoneMatch()
    {
        var requirement = new RoleRequirement(AppRoles.Admin);
        var user = CreateUserWithMultipleRoles(AppRoles.Requestor, AppRoles.Operator);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_Succeeds_WhenUserHasMultipleRoles_WithCompositeRequirement()
    {
        var requirement = new RoleRequirement(AppRoles.Admin, AppRoles.Operator);
        var user = CreateUserWithMultipleRoles(AppRoles.Requestor, AppRoles.Operator);
        var context = CreateContext(requirement, user);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreateUserWithMultipleRoles(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test@example.com")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
