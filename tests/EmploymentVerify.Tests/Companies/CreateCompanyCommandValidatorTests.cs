using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Application.Companies.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EmploymentVerify.Tests.CompanyTests;

public class CreateCompanyCommandValidatorTests
{
    private readonly CreateCompanyCommandValidator _validator = new();

    private static CreateCompanyCommand ValidCommand() => new(
        Name: "Acme Holdings (Pty) Ltd",
        RegistrationNumber: "2020/123456/07",
        HrContactName: "Jane Smith",
        HrEmail: "hr@acme.co.za",
        HrPhone: "+27821234567",
        Address: "123 Main Street",
        City: "Cape Town",
        Province: "Western Cape",
        PostalCode: "8001",
        ForceCall: false);

    [Fact]
    public async Task Should_Pass_When_All_Fields_Are_Valid()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_Name_Is_Empty(string? name)
    {
        var command = ValidCommand() with { Name = name! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_RegistrationNumber_Is_Empty(string? regNumber)
    {
        var command = ValidCommand() with { RegistrationNumber = regNumber! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("2020-123456-07")]
    [InlineData("ABCD/123456/07")]
    public async Task Should_Fail_When_RegistrationNumber_Has_Invalid_Format(string regNumber)
    {
        var command = ValidCommand() with { RegistrationNumber = regNumber };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Theory]
    [InlineData("2020/123456/07")]
    [InlineData("2023/1234567/01")]
    [InlineData("1999/12345/23")]
    public async Task Should_Pass_When_RegistrationNumber_Has_Valid_Format(string regNumber)
    {
        var command = ValidCommand() with { RegistrationNumber = regNumber };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_HrContactName_Is_Empty(string? name)
    {
        var command = ValidCommand() with { HrContactName = name! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrContactName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_HrEmail_Is_Empty(string? email)
    {
        var command = ValidCommand() with { HrEmail = email! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrEmail);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    public async Task Should_Fail_When_HrEmail_Is_Invalid(string email)
    {
        var command = ValidCommand() with { HrEmail = email };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Should_Fail_When_HrPhone_Is_Empty(string? phone)
    {
        var command = ValidCommand() with { HrPhone = phone! };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrPhone);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("+1234567890")]
    [InlineData("082-123-4567")]
    public async Task Should_Fail_When_HrPhone_Has_Invalid_SA_Format(string phone)
    {
        var command = ValidCommand() with { HrPhone = phone };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrPhone);
    }

    [Theory]
    [InlineData("+27821234567")]
    [InlineData("0821234567")]
    public async Task Should_Pass_When_HrPhone_Has_Valid_SA_Format(string phone)
    {
        var command = ValidCommand() with { HrPhone = phone };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.HrPhone);
    }

    [Fact]
    public async Task Should_Pass_When_Optional_Address_Fields_Are_Null()
    {
        var command = ValidCommand() with
        {
            Address = null,
            City = null,
            Province = null,
            PostalCode = null
        };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Should_Pass_When_ForceCall_Is_True()
    {
        var command = ValidCommand() with { ForceCall = true };
        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
