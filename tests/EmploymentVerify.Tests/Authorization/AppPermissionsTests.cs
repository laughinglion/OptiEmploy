using EmploymentVerify.Domain.Constants;

namespace EmploymentVerify.Tests.Authorization;

public class AppPermissionsTests
{
    [Fact]
    public void CompanyActions_RequireAdmin()
    {
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CompanyCreate);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CompanyEdit);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CompanyDelete);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CompanyView);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CompanyToggleForceCall);
    }

    [Fact]
    public void UserManagementActions_RequireAdmin()
    {
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.UserCreate);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.UserDeactivate);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.UserAssignRole);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.UserManage);
    }

    [Fact]
    public void CreditActions_RequireAdmin()
    {
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CreditAdd);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.CreditView);
    }

    [Fact]
    public void VerificationSubmission_RequiresRequestor()
    {
        Assert.Equal(AuthorizationPolicies.RequireRequestor, AppPermissions.VerificationSubmit);
        Assert.Equal(AuthorizationPolicies.RequireRequestor, AppPermissions.VerificationViewOwn);
    }

    [Fact]
    public void VerificationReports_RequireAdminOrRequestor()
    {
        Assert.Equal(AuthorizationPolicies.RequireAdminOrRequestor, AppPermissions.VerificationViewReport);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrRequestor, AppPermissions.VerificationViewStatus);
    }

    [Fact]
    public void OperatorActions_RequireAdminOrOperator()
    {
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, AppPermissions.OperatorQueueView);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, AppPermissions.OperatorRecordOutcome);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, AppPermissions.OperatorUpdateStatus);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, AppPermissions.OperatorAddNotes);
    }

    [Fact]
    public void AdminOverview_RequiresAdmin()
    {
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.VerificationViewAll);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, AppPermissions.StatisticsView);
    }

    [Fact]
    public void ProfileManage_RequiresAnyRole()
    {
        Assert.Equal(AuthorizationPolicies.RequireAnyRole, AppPermissions.ProfileManage);
    }

    [Fact]
    public void RoutePermissions_ContainsAllExpectedRoutes()
    {
        var routes = AppPermissions.RoutePermissions;

        // Admin routes
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/companies"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/companies/create"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/companies/edit"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/users"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/users/create"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/credits"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/verifications"]);
        Assert.Equal(AuthorizationPolicies.RequireAdmin, routes["/admin/statistics"]);

        // Requestor routes
        Assert.Equal(AuthorizationPolicies.RequireRequestor, routes["/verifications/submit"]);
        Assert.Equal(AuthorizationPolicies.RequireRequestor, routes["/verifications/history"]);

        // Admin + Requestor routes
        Assert.Equal(AuthorizationPolicies.RequireAdminOrRequestor, routes["/verifications/status"]);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrRequestor, routes["/verifications/report"]);

        // Operator routes
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, routes["/operator/queue"]);
        Assert.Equal(AuthorizationPolicies.RequireAdminOrOperator, routes["/operator/verify"]);

        // Shared routes
        Assert.Equal(AuthorizationPolicies.RequireAnyRole, routes["/profile"]);
        Assert.Equal(AuthorizationPolicies.RequireAnyRole, routes["/dashboard"]);
    }

    [Fact]
    public void RoutePermissions_AllPoliciesAreValid()
    {
        foreach (var (route, policy) in AppPermissions.RoutePermissions)
        {
            Assert.Contains(policy, AuthorizationPolicies.All);
        }
    }

    [Fact]
    public void RoutePermissions_IsCaseInsensitive()
    {
        // Verify dictionary uses case-insensitive comparison
        Assert.Equal(
            AppPermissions.RoutePermissions["/admin/companies"],
            AppPermissions.RoutePermissions["/Admin/Companies"]);
    }
}
