using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Application.Companies.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EmploymentVerify.Tests.Companies;

public class CompanyValidatorTests
{
    private readonly CreateCompanyCommandValidator _createValidator = new();
    private readonly UpdateCompanyCommandValidator _updateValidator = new();

    private static CreateCompanyCommand ValidCreateCommand() => new(
        Name: "Test Corp",
        RegistrationNumber: "2020/123456/07",
        HrContactName: "Jane Smith",
        HrEmail: "hr@test.co.za",
        HrPhone: "+27821234567",
        Address: "123 Main St",
        City: "Cape Town",
        Province: "Western Cape",
        PostalCode: "8001",
        ForceCall: false);

    // --- CreateCompanyCommand validation ---

    [Fact]
    public async Task Create_ValidCommand_PassesValidation()
    {
        var result = await _createValidator.TestValidateAsync(ValidCreateCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Create_EmptyName_FailsValidation(string? name)
    {
        var command = ValidCreateCommand() with { Name = name! };
        var result = await _createValidator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INVALID")]
    [InlineData("2020/12/07")]      // too few digits
    [InlineData("20201234567/07")]   // missing slashes
    public async Task Create_InvalidRegistrationNumber_FailsValidation(string regNumber)
    {
        var command = ValidCreateCommand() with { RegistrationNumber = regNumber };
        var result = await _createValidator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Theory]
    [InlineData("2020/12345/07")]
    [InlineData("2020/123456/07")]
    [InlineData("2020/1234567/07")]
    public async Task Create_ValidRegistrationNumber_PassesValidation(string regNumber)
    {
        var command = ValidCreateCommand() with { RegistrationNumber = regNumber };
        var result = await _createValidator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task Create_InvalidHrEmail_FailsValidation(string email)
    {
        var command = ValidCreateCommand() with { HrEmail = email };
        var result = await _createValidator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrEmail);
    }

    [Theory]
    [InlineData("+27821234567")]
    [InlineData("0821234567")]
    public async Task Create_ValidSAPhoneNumber_PassesValidation(string phone)
    {
        var command = ValidCreateCommand() with { HrPhone = phone };
        var result = await _createValidator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.HrPhone);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("+1234567890")]
    [InlineData("")]
    public async Task Create_InvalidPhoneNumber_FailsValidation(string phone)
    {
        var command = ValidCreateCommand() with { HrPhone = phone };
        var result = await _createValidator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.HrPhone);
    }

    // --- UpdateCompanyCommand validation ---

    [Fact]
    public async Task Update_EmptyCompanyId_FailsValidation()
    {
        var command = new UpdateCompanyCommand(
            CompanyId: Guid.Empty,
            Name: "Test", RegistrationNumber: "2020/123456/07",
            HrContactName: "Jane", HrEmail: "hr@test.co.za", HrPhone: "+27821234567",
            Address: null, City: null, Province: null, PostalCode: null,
            ForceCall: false, IsActive: true);

        var result = await _updateValidator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.CompanyId);
    }

    [Fact]
    public async Task Update_ValidCommand_PassesValidation()
    {
        var command = new UpdateCompanyCommand(
            CompanyId: Guid.NewGuid(),
            Name: "Updated Corp", RegistrationNumber: "2020/654321/07",
            HrContactName: "Bob", HrEmail: "hr@updated.co.za", HrPhone: "0821234567",
            Address: "456 New St", City: "Johannesburg", Province: "Gauteng", PostalCode: "2000",
            ForceCall: true, IsActive: true);

        var result = await _updateValidator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
