using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Application.Verifications.Validators;
using EmploymentVerify.Domain.Enums;
using FluentAssertions;

namespace EmploymentVerify.Tests.Verifications;

public class RecordHrResponseCommandValidatorTests
{
    private readonly RecordHrResponseCommandValidator _validator = new();

    [Fact]
    public void Valid_Confirmed_Response_Should_Pass()
    {
        var command = new RecordHrResponseCommand(
            "token123", "HR Manager", ResponseType.Confirmed,
            "Engineer", new DateOnly(2020, 1, 1), null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Denied_Response_Without_JobTitle_Should_Pass()
    {
        var command = new RecordHrResponseCommand(
            "token123", "HR Manager", ResponseType.Denied,
            null, null, null, null, "Not employed here");

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Confirmed_Without_JobTitle_Should_Fail()
    {
        var command = new RecordHrResponseCommand(
            "token123", "HR Manager", ResponseType.Confirmed,
            null, new DateOnly(2020, 1, 1), null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmedJobTitle");
    }

    [Fact]
    public void Confirmed_Without_StartDate_Should_Fail()
    {
        var command = new RecordHrResponseCommand(
            "token123", "HR Manager", ResponseType.Confirmed,
            "Engineer", null, null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmedStartDate");
    }

    [Fact]
    public void Empty_Token_Should_Fail()
    {
        var command = new RecordHrResponseCommand(
            "", "HR Manager", ResponseType.Confirmed,
            "Engineer", new DateOnly(2020, 1, 1), null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token");
    }

    [Fact]
    public void Empty_RespondedBy_Should_Fail()
    {
        var command = new RecordHrResponseCommand(
            "token123", "", ResponseType.Denied,
            null, null, null, null, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RespondedBy");
    }

    [Fact]
    public void Notes_Exceeding_Max_Length_Should_Fail()
    {
        var command = new RecordHrResponseCommand(
            "token123", "HR Manager", ResponseType.Denied,
            null, null, null, null, new string('x', 2001));

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }
}
