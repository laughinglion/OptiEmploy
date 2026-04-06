using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Application.Verifications.Validators;
using EmploymentVerify.Domain.Enums;
using FluentAssertions;

namespace EmploymentVerify.Tests.Verifications;

public class RecordOperatorCallCommandValidatorTests
{
    private readonly RecordOperatorCallCommandValidator _validator = new();

    [Fact]
    public void Valid_Confirmed_Call_Should_Pass()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Confirmed,
            "Spoke with HR manager", "Engineer", new DateOnly(2020, 1, 1), null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Denied_Call_Without_JobTitle_Should_Pass()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Denied,
            "HR confirmed employee never worked here", null, null, null, null, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Unreachable_Without_JobTitle_Should_Pass()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Unreachable,
            "No answer after 3 attempts", null, null, null, null, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Confirmed_Without_JobTitle_Should_Fail()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Confirmed,
            "Spoke with HR", null, new DateOnly(2020, 1, 1), null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmedJobTitle");
    }

    [Fact]
    public void Confirmed_Without_StartDate_Should_Fail()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Confirmed,
            "Spoke with HR", "Engineer", null, null, true, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConfirmedStartDate");
    }

    [Fact]
    public void Empty_Notes_Should_Fail()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Unreachable,
            "", null, null, null, null, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    [Fact]
    public void Notes_Exceeding_Max_Length_Should_Fail()
    {
        var command = new RecordOperatorCallCommand(
            Guid.NewGuid(), Guid.NewGuid(), CallOutcome.Denied,
            new string('x', 2001), null, null, null, null, null);

        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }
}
