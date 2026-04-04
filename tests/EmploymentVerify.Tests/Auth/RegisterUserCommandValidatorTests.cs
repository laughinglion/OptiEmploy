using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Application.Auth.Validators;
using EmploymentVerify.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EmploymentVerify.Tests.Auth;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand ValidCommand() => new(
        Email: "test@example.com",
        Password: "SecureP@ss1",
        FullName: "John Doe",
        CompanyName: "Acme Corp");

    [Fact]
    public async Task Should_Pass_When_All_Fields_Are_Valid()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_Email_Is_Empty(string? email)
    {
        var command = ValidCommand() with { Email = email! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain")]
    public async Task Should_Fail_When_Email_Is_Invalid(string email)
    {
        var command = ValidCommand() with { Email = email };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Should_Fail_When_Password_Is_Too_Short()
    {
        var command = ValidCommand() with { Password = "Ab1!" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Should_Fail_When_Password_Has_No_Uppercase()
    {
        var command = ValidCommand() with { Password = "securep@ss1" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Should_Fail_When_Password_Has_No_Lowercase()
    {
        var command = ValidCommand() with { Password = "SECUREP@SS1" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Should_Fail_When_Password_Has_No_Digit()
    {
        var command = ValidCommand() with { Password = "SecureP@ssword" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Should_Fail_When_Password_Has_No_Special_Character()
    {
        var command = ValidCommand() with { Password = "SecurePass1" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_FullName_Is_Empty(string? fullName)
    {
        var command = ValidCommand() with { FullName = fullName! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public async Task Should_Fail_When_FullName_Is_Too_Short()
    {
        var command = ValidCommand() with { FullName = "A" };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public async Task Should_Allow_Null_CompanyName()
    {
        var command = ValidCommand() with { CompanyName = null };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.CompanyName);
    }

    [Fact]
    public async Task Should_Allow_Null_Role()
    {
        var command = ValidCommand() with { Role = null };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Requestor)]
    [InlineData(UserRole.Operator)]
    public async Task Should_Pass_When_Role_Is_Valid(UserRole role)
    {
        var command = ValidCommand() with { Role = role };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public async Task Should_Fail_When_Role_Is_Invalid_Enum_Value()
    {
        var command = ValidCommand() with { Role = (UserRole)999 };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
