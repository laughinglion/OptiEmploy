using System.Reflection;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Web.Components.Pages;
using EmploymentVerify.Web.Components.Pages.Account;
using EmploymentVerify.Web.Components.Pages.Admin;
using EmploymentVerify.Web.Components.Pages.Operations;
using EmploymentVerify.Web.Components.Pages.Verifications;
using Microsoft.AspNetCore.Authorization;

namespace EmploymentVerify.Tests.Web;

public class PageAuthorizationTests
{
    /// <summary>
    /// Extracts the authorization policy name from the [Authorize] attribute on a Razor component.
    /// </summary>
    private static string? GetAuthPolicy(Type pageType)
    {
        var attr = pageType.GetCustomAttribute<AuthorizeAttribute>();
        return attr?.Policy;
    }

    private static bool HasAuthorize(Type pageType)
    {
        return pageType.GetCustomAttribute<AuthorizeAttribute>() is not null;
    }

    private static bool HasAllowAnonymous(Type pageType)
    {
        return pageType.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
    }

    // --- Admin pages require Admin policy ---

    [Theory]
    [InlineData(typeof(UserManagement))]
    [InlineData(typeof(EmploymentVerify.Web.Components.Pages.Admin.Companies))]
    [InlineData(typeof(AuditLog))]
    public void AdminPages_RequireAdminPolicy(Type pageType)
    {
        var policy = GetAuthPolicy(pageType);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, policy);
    }

    // --- Verification pages require Admin or Requestor policy ---

    [Theory]
    [InlineData(typeof(SubmitRequest))]
    [InlineData(typeof(MyRequests))]
    public void VerificationPages_RequireAdminOrRequestorPolicy(Type pageType)
    {
        var policy = GetAuthPolicy(pageType);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrRequestor, policy);
    }

    // --- Operations pages require Admin or Operator policy ---

    [Theory]
    [InlineData(typeof(WorkQueue))]
    [InlineData(typeof(AllVerifications))]
    public void OperationPages_RequireAdminOrOperatorPolicy(Type pageType)
    {
        var policy = GetAuthPolicy(pageType);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, policy);
    }

    // --- Home/Dashboard requires any valid role ---

    [Fact]
    public void HomePage_RequiresAnyRolePolicy()
    {
        Assert.True(HasAuthorize(typeof(Home)));
        var policy = GetAuthPolicy(typeof(Home));
        Assert.Equal(AuthorizationPolicies.RequireAnyRole, policy);
    }

    // --- Public pages allow anonymous ---

    [Theory]
    [InlineData(typeof(Login))]
    [InlineData(typeof(Register))]
    [InlineData(typeof(AccessDenied))]
    [InlineData(typeof(NotFound))]
    public void PublicPages_AllowAnonymous(Type pageType)
    {
        Assert.True(HasAllowAnonymous(pageType));
    }

    [Fact]
    public void ErrorPage_AllowsAnonymous()
    {
        Assert.True(HasAllowAnonymous(typeof(Error)));
    }
}
