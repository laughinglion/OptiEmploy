using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Application.Auth.Validators;
using EmploymentVerify.Domain.Enums;
using FluentValidation.TestHelper;

namespace EmploymentVerify.Tests.Auth;

public class AssignUserRoleCommandValidatorTests
{
    private readonly AssignUserRoleCommandValidator _validator = new();

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Requestor)]
    [InlineData(UserRole.Operator)]
    public async Task Should_Pass_When_Role_Is_Valid(UserRole role)
    {
        var command = new AssignUserRoleCommand(Guid.NewGuid(), role);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Fail_When_UserId_Is_Empty()
    {
        var command = new AssignUserRoleCommand(Guid.Empty, UserRole.Admin);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public async Task Should_Fail_When_Role_Is_Invalid()
    {
        var command = new AssignUserRoleCommand(Guid.NewGuid(), (UserRole)999);
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
