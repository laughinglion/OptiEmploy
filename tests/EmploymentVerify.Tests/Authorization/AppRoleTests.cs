using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Domain.Enums;

namespace EmploymentVerify.Tests.Authorization;

public class AppRoleTests
{
    [Fact]
    public void UserRole_HasThreeMembers()
    {
        var values = Enum.GetValues<UserRole>();
        Assert.Equal(3, values.Length);
    }

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.Requestor, "Requestor")]
    [InlineData(UserRole.Operator, "Operator")]
    public void UserRole_NamesMatchStringConstants(UserRole role, string expected)
    {
        Assert.Equal(expected, role.ToString());
    }

    [Fact]
    public void AppRoles_AllContainsExactlyThreeRoles()
    {
        Assert.Equal(3, AppRoles.All.Count);
        Assert.Contains(AppRoles.Admin, AppRoles.All);
        Assert.Contains(AppRoles.Requestor, AppRoles.All);
        Assert.Contains(AppRoles.Operator, AppRoles.All);
    }

    [Fact]
    public void AppRoles_StringConstants_MatchEnumNames()
    {
        Assert.Equal(nameof(UserRole.Admin), AppRoles.Admin);
        Assert.Equal(nameof(UserRole.Requestor), AppRoles.Requestor);
        Assert.Equal(nameof(UserRole.Operator), AppRoles.Operator);
    }
}
