using EmploymentVerify.Domain.Entities;
using FluentAssertions;

namespace EmploymentVerify.Tests.CompanyTests;

public class CompanyEntityTests
{
    [Fact]
    public void Company_Should_Have_All_Required_Fields()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme Holdings (Pty) Ltd",
            RegistrationNumber = "2020/123456/07",
            HrContactName = "Jane Smith",
            HrEmail = "hr@acme.co.za",
            HrPhone = "+27821234567",
            ForceCall = false
        };

        company.Id.Should().NotBeEmpty();
        company.Name.Should().Be("Acme Holdings (Pty) Ltd");
        company.RegistrationNumber.Should().Be("2020/123456/07");
        company.HrContactName.Should().Be("Jane Smith");
        company.HrEmail.Should().Be("hr@acme.co.za");
        company.HrPhone.Should().Be("+27821234567");
        company.ForceCall.Should().BeFalse();
    }

    [Fact]
    public void Company_Should_Default_IsActive_To_True()
    {
        var company = new Company();
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Company_Should_Default_ForceCall_To_False()
    {
        var company = new Company();
        company.ForceCall.Should().BeFalse();
    }

    [Fact]
    public void Company_Should_Support_ForceCall_Flag()
    {
        var company = new Company { ForceCall = true };
        company.ForceCall.Should().BeTrue();
    }

    [Fact]
    public void Company_Should_Have_Optional_Address_Fields()
    {
        var company = new Company
        {
            Address = "123 Main Street",
            City = "Johannesburg",
            Province = "Gauteng",
            PostalCode = "2001"
        };

        company.Address.Should().Be("123 Main Street");
        company.City.Should().Be("Johannesburg");
        company.Province.Should().Be("Gauteng");
        company.PostalCode.Should().Be("2001");
    }

    [Fact]
    public void Company_Should_Allow_Null_Optional_Fields()
    {
        var company = new Company();
        company.Address.Should().BeNull();
        company.City.Should().BeNull();
        company.Province.Should().BeNull();
        company.PostalCode.Should().BeNull();
        company.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Company_Should_Set_CreatedAt_To_UtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var company = new Company();
        var after = DateTime.UtcNow.AddSeconds(1);

        company.CreatedAt.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public void Company_Should_Default_IsVerified_To_False()
    {
        var company = new Company();
        company.IsVerified.Should().BeFalse();
    }
}
