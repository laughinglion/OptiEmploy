using System.Security.Claims;
using EmploymentVerify.Api.Filters;
using EmploymentVerify.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace EmploymentVerify.Tests.Api;

public class RoleAuthorizationFilterTests
{
    private static DefaultHttpContext CreateHttpContext(string? role = null, bool authenticated = true)
    {
        var claims = new List<Claim>();
        if (authenticated)
        {
            claims.Add(new Claim(ClaimTypes.Name, "test@example.com"));
            if (role is not null)
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = authenticated
            ? new ClaimsIdentity(claims, "TestAuth")
            : new ClaimsIdentity();

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return context;
    }

    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        return new DefaultEndpointFilterInvocationContext(httpContext);
    }

    /// <summary>
    /// Extracts StatusCode from a JsonHttpResult via reflection (since the generic type is anonymous).
    /// </summary>
    private static int? GetStatusCodeFromResult(object? result)
    {
        if (result is null) return null;
        var type = result.GetType();
        var prop = type.GetProperty("StatusCode");
        return prop?.GetValue(result) as int?;
    }

    [Fact]
    public async Task Filter_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        var filter = new RoleAuthorizationFilter(AppRoles.Admin);
        var httpContext = CreateHttpContext(authenticated: false);
        var context = CreateFilterContext(httpContext);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(Results.Ok()));

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, GetStatusCodeFromResult(result));
    }

    [Fact]
    public async Task Filter_ReturnsForbidden_WhenUserHasWrongRole()
    {
        var filter = new RoleAuthorizationFilter(AppRoles.Admin);
        var httpContext = CreateHttpContext(role: AppRoles.Requestor);
        var context = CreateFilterContext(httpContext);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(Results.Ok()));

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCodeFromResult(result));
    }

    [Fact]
    public async Task Filter_ReturnsForbidden_WhenUserHasNoRoleClaim()
    {
        var filter = new RoleAuthorizationFilter(AppRoles.Admin);
        var httpContext = CreateHttpContext(role: null);
        var context = CreateFilterContext(httpContext);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(Results.Ok()));

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCodeFromResult(result));
    }

    [Fact]
    public async Task Filter_CallsNext_WhenUserHasCorrectRole()
    {
        var filter = new RoleAuthorizationFilter(AppRoles.Admin);
        var httpContext = CreateHttpContext(role: AppRoles.Admin);
        var context = CreateFilterContext(httpContext);
        var expectedResult = Results.Ok("success");

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(expectedResult));

        Assert.Same(expectedResult, result);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.Operator)]
    public async Task Filter_AllowsAnyOfMultipleRoles(string role)
    {
        var filter = new RoleAuthorizationFilter(AppRoles.Admin, AppRoles.Operator);
        var httpContext = CreateHttpContext(role: role);
        var context = CreateFilterContext(httpContext);
        var expectedResult = Results.Ok("success");

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(expectedResult));

        Assert.Same(expectedResult, result);
    }

    [Fact]
    public async Task Filter_DeniesRoleNotInAllowedList()
    {
        var filter = new RoleAuthorizationFilter(AppRoles.Admin, AppRoles.Operator);
        var httpContext = CreateHttpContext(role: AppRoles.Requestor);
        var context = CreateFilterContext(httpContext);

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(Results.Ok()));

        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status403Forbidden, GetStatusCodeFromResult(result));
    }

    [Fact]
    public async Task Filter_IsCaseInsensitive()
    {
        var filter = new RoleAuthorizationFilter("admin");
        var httpContext = CreateHttpContext(role: "Admin");
        var context = CreateFilterContext(httpContext);
        var expectedResult = Results.Ok("success");

        var result = await filter.InvokeAsync(context, _ => new ValueTask<object?>(expectedResult));

        Assert.Same(expectedResult, result);
    }

    [Fact]
    public void Filter_ThrowsIfNoRolesSpecified()
    {
        Assert.Throws<ArgumentException>(() => new RoleAuthorizationFilter());
    }
}
