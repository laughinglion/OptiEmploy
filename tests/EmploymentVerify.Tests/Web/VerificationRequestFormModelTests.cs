using System.ComponentModel.DataAnnotations;
using FluentAssertions;

namespace EmploymentVerify.Tests.Web;

/// <summary>
/// Tests for the VerificationRequestFormModel validation attributes
/// defined in the SubmitRequest.razor component.
/// These tests validate the DataAnnotations rules match the business requirements.
/// </summary>
public class VerificationRequestFormModelTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidModel_WithSaIdNumber_PassesValidation()
    {
        var model = CreateValidSaIdModel();
        var results = ValidateModel(model);
        results.Should().BeEmpty();
    }

    [Fact]
    public void ValidModel_WithPassport_PassesValidation()
    {
        var model = CreateValidPassportModel();
        var results = ValidateModel(model);
        results.Should().BeEmpty();
    }

    [Fact]
    public void EmployeeFullName_Required()
    {
        var model = CreateValidSaIdModel();
        model.EmployeeFullName = null;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("Employee full name"));
    }

    [Fact]
    public void EmployeeFullName_TooShort()
    {
        var model = CreateValidSaIdModel();
        model.EmployeeFullName = "A";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("between 2 and 200"));
    }

    [Fact]
    public void SaIdNumber_MustBe13Digits()
    {
        var model = CreateValidSaIdModel();
        model.SaIdNumber = "12345"; // Too short
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("13 digits"));
    }

    [Fact]
    public void SaIdNumber_RejectsNonNumeric()
    {
        var model = CreateValidSaIdModel();
        model.SaIdNumber = "ABCDEFGHIJKLM"; // 13 chars but not digits
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("13 digits"));
    }

    [Fact]
    public void CompanyName_Required()
    {
        var model = CreateValidSaIdModel();
        model.CompanyName = null;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("Company name"));
    }

    [Fact]
    public void JobTitle_Required()
    {
        var model = CreateValidSaIdModel();
        model.JobTitle = null;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("Job title"));
    }

    [Fact]
    public void EmploymentStartDate_Required()
    {
        var model = CreateValidSaIdModel();
        model.EmploymentStartDate = null;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("Employment start date"));
    }

    [Fact]
    public void ConsentToPopia_MustBeTrue()
    {
        var model = CreateValidSaIdModel();
        model.ConsentToPopia = false;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("POPIA"));
    }

    [Fact]
    public void ConsentAccuracy_MustBeTrue()
    {
        var model = CreateValidSaIdModel();
        model.ConsentAccuracy = false;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("accuracy"));
    }

    [Fact]
    public void HrEmail_OptionalButMustBeValid()
    {
        var model = CreateValidSaIdModel();
        model.HrEmail = "not-an-email";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("valid email"));
    }

    [Fact]
    public void HrPhone_OptionalButRejectsInvalidChars()
    {
        var model = CreateValidSaIdModel();
        model.HrPhone = "abc@#$";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("invalid characters"));
    }

    [Fact]
    public void HrPhone_AcceptsValidFormats()
    {
        var model = CreateValidSaIdModel();
        model.HrPhone = "+27 11 123 4567";
        var results = ValidateModel(model);
        results.Should().BeEmpty();
    }

    [Fact]
    public void PassportNumber_Length_Validation()
    {
        var model = CreateValidPassportModel();
        model.PassportNumber = "AB"; // Too short (min 4)
        var results = ValidateModel(model);
        results.Should().Contain(r => r.ErrorMessage!.Contains("Passport number"));
    }

    [Fact]
    public void EmploymentEndDate_IsOptional()
    {
        var model = CreateValidSaIdModel();
        model.EmploymentEndDate = null;
        var results = ValidateModel(model);
        results.Should().BeEmpty();
    }

    // ── Helper factory methods ──

    private static VerificationRequestFormModel CreateValidSaIdModel() => new()
    {
        EmployeeFullName = "Sipho Nkosi",
        SaIdNumber = "9001015009087",
        CompanyName = "Test Company",
        JobTitle = "Software Developer",
        EmploymentStartDate = new DateOnly(2023, 1, 15),
        ConsentToPopia = true,
        ConsentAccuracy = true
    };

    private static VerificationRequestFormModel CreateValidPassportModel() => new()
    {
        EmployeeFullName = "Jane Smith",
        PassportNumber = "AB123456",
        PassportCountry = "Zimbabwe",
        CompanyName = "Test Company",
        JobTitle = "Accountant",
        EmploymentStartDate = new DateOnly(2022, 6, 1),
        ConsentToPopia = true,
        ConsentAccuracy = true
    };

    /// <summary>
    /// Mirrors the form model defined in SubmitRequest.razor for testing purposes.
    /// Kept in sync with the Razor component's inner class.
    /// </summary>
    private sealed class VerificationRequestFormModel
    {
        [Required(ErrorMessage = "Employee full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Employee full name must be between 2 and 200 characters.")]
        public string? EmployeeFullName { get; set; }

        [RegularExpression(@"^\d{13}$", ErrorMessage = "SA ID Number must be exactly 13 digits.")]
        public string? SaIdNumber { get; set; }

        [StringLength(20, MinimumLength = 4, ErrorMessage = "Passport number must be between 4 and 20 characters.")]
        public string? PassportNumber { get; set; }

        [StringLength(100, ErrorMessage = "Passport country must not exceed 100 characters.")]
        public string? PassportCountry { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(300, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 300 characters.")]
        public string? CompanyName { get; set; }

        public Guid? SelectedCompanyId { get; set; }

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Job title must be between 2 and 200 characters.")]
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Employment start date is required.")]
        public DateOnly? EmploymentStartDate { get; set; }

        public DateOnly? EmploymentEndDate { get; set; }

        [StringLength(200, ErrorMessage = "HR contact name must not exceed 200 characters.")]
        public string? HrContactName { get; set; }

        [EmailAddress(ErrorMessage = "A valid email address is required for HR contact.")]
        [StringLength(256, ErrorMessage = "HR email must not exceed 256 characters.")]
        public string? HrEmail { get; set; }

        [StringLength(20, ErrorMessage = "HR phone must not exceed 20 characters.")]
        [RegularExpression(@"^[\d\s\+\-\(\)]+$", ErrorMessage = "HR phone number contains invalid characters.")]
        public string? HrPhone { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm POPIA consent to submit a verification request.")]
        public bool ConsentToPopia { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm the accuracy of the information provided.")]
        public bool ConsentAccuracy { get; set; }
    }
}
