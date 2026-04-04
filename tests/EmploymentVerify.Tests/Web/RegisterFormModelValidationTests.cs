using System.ComponentModel.DataAnnotations;
using FluentAssertions;

namespace EmploymentVerify.Tests.Web;

public class RegisterFormModelValidationTests
{
    private static RegisterFormModel ValidModel() => new()
    {
        FullName = "John Doe",
        Email = "john@example.com",
        Password = "SecureP@ss1",
        ConfirmPassword = "SecureP@ss1",
        CompanyName = "Acme Corp",
        ConsentToPopia = true
    };

    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Should_Pass_When_All_Fields_Are_Valid()
    {
        var model = ValidModel();
        var results = ValidateModel(model);
        results.Should().BeEmpty();
    }

    [Fact]
    public void Should_Fail_When_FullName_Is_Empty()
    {
        var model = ValidModel();
        model.FullName = "";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("FullName"));
    }

    [Fact]
    public void Should_Fail_When_FullName_Is_Too_Short()
    {
        var model = ValidModel();
        model.FullName = "A";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("FullName"));
    }

    [Fact]
    public void Should_Fail_When_FullName_Exceeds_Max_Length()
    {
        var model = ValidModel();
        model.FullName = new string('A', 201);
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("FullName"));
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Empty()
    {
        var model = ValidModel();
        model.Email = "";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        var model = ValidModel();
        model.Email = "notanemail";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Should_Fail_When_Email_Exceeds_Max_Length()
    {
        var model = ValidModel();
        model.Email = new string('a', 252) + "@x.com"; // 258 chars total, exceeds 256
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Empty()
    {
        var model = ValidModel();
        model.Password = "";
        model.ConfirmPassword = "";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Should_Fail_When_Password_Is_Too_Short()
    {
        var model = ValidModel();
        model.Password = "Ab1!";
        model.ConfirmPassword = "Ab1!";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Should_Fail_When_Password_Lacks_Complexity()
    {
        var model = ValidModel();
        model.Password = "simplepas";
        model.ConfirmPassword = "simplepas";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Should_Fail_When_Password_Exceeds_Max_Length()
    {
        var model = ValidModel();
        model.Password = "A@1a" + new string('x', 125);
        model.ConfirmPassword = model.Password;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Should_Fail_When_ConfirmPassword_Does_Not_Match()
    {
        var model = ValidModel();
        model.ConfirmPassword = "DifferentP@ss1";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("ConfirmPassword"));
    }

    [Fact]
    public void Should_Fail_When_ConfirmPassword_Is_Empty()
    {
        var model = ValidModel();
        model.ConfirmPassword = "";
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("ConfirmPassword"));
    }

    [Fact]
    public void Should_Pass_When_CompanyName_Is_Null()
    {
        var model = ValidModel();
        model.CompanyName = null;
        var results = ValidateModel(model);
        results.Should().NotContain(r => r.MemberNames.Contains("CompanyName"));
    }

    [Fact]
    public void Should_Fail_When_CompanyName_Exceeds_Max_Length()
    {
        var model = ValidModel();
        model.CompanyName = new string('C', 201);
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("CompanyName"));
    }

    [Fact]
    public void Should_Fail_When_POPIA_Consent_Is_Not_Given()
    {
        var model = ValidModel();
        model.ConsentToPopia = false;
        var results = ValidateModel(model);
        results.Should().Contain(r => r.MemberNames.Contains("ConsentToPopia"));
    }

    [Fact]
    public void Should_Pass_When_POPIA_Consent_Is_Given()
    {
        var model = ValidModel();
        model.ConsentToPopia = true;
        var results = ValidateModel(model);
        results.Should().NotContain(r => r.MemberNames.Contains("ConsentToPopia"));
    }

    /// <summary>
    /// Mirror of the private RegisterFormModel class used in Register.razor.
    /// Validation attributes must match the component's model exactly.
    /// </summary>
    private sealed class RegisterFormModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,128}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [StringLength(200, ErrorMessage = "Company name must not exceed 200 characters.")]
        public string? CompanyName { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must consent to POPIA to create an account.")]
        public bool ConsentToPopia { get; set; }
    }
}
