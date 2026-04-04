using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Companies;

public class CreateCompanyCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCompanyAndReturnsDto()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new CreateCompanyCommandHandler(context);
        var command = new CreateCompanyCommand(
            Name: "Acme Corp",
            RegistrationNumber: "2020/123456/07",
            HrContactName: "Jane Smith",
            HrEmail: "HR@acme.co.za",
            HrPhone: "+27821234567",
            Address: "123 Main St",
            City: "Cape Town",
            Province: "Western Cape",
            PostalCode: "8001",
            ForceCall: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Acme Corp");
        result.RegistrationNumber.Should().Be("2020/123456/07");
        result.HrContactName.Should().Be("Jane Smith");
        result.HrEmail.Should().Be("hr@acme.co.za"); // lowercased
        result.HrPhone.Should().Be("+27821234567");
        result.Address.Should().Be("123 Main St");
        result.City.Should().Be("Cape Town");
        result.Province.Should().Be("Western Cape");
        result.PostalCode.Should().Be("8001");
        result.ForceCall.Should().BeFalse();
        result.IsVerified.Should().BeFalse();
        result.IsActive.Should().BeTrue();

        // Verify persisted
        var saved = await context.Companies.FirstAsync();
        saved.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task Handle_DuplicateRegistrationNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new CreateCompanyCommandHandler(context);
        var command = new CreateCompanyCommand(
            Name: "First Corp",
            RegistrationNumber: "2020/123456/07",
            HrContactName: "Jane",
            HrEmail: "hr@first.co.za",
            HrPhone: "+27821234567",
            Address: null, City: null, Province: null, PostalCode: null,
            ForceCall: false);

        await handler.Handle(command, CancellationToken.None);

        var duplicateCommand = new CreateCompanyCommand(
            Name: "Second Corp",
            RegistrationNumber: "2020/123456/07",
            HrContactName: "Bob",
            HrEmail: "hr@second.co.za",
            HrPhone: "+27829876543",
            Address: null, City: null, Province: null, PostalCode: null,
            ForceCall: false);

        // Act & Assert
        var act = () => handler.Handle(duplicateCommand, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_TrimsAndNormalizesInputs()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new CreateCompanyCommandHandler(context);
        var command = new CreateCompanyCommand(
            Name: "  Acme Corp  ",
            RegistrationNumber: "  2020/123456/07  ",
            HrContactName: "  Jane Smith  ",
            HrEmail: "  HR@Acme.CO.ZA  ",
            HrPhone: "  +27821234567  ",
            Address: "  123 Main  ",
            City: "  Cape Town  ",
            Province: "  Western Cape  ",
            PostalCode: "  8001  ",
            ForceCall: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Acme Corp");
        result.RegistrationNumber.Should().Be("2020/123456/07");
        result.HrContactName.Should().Be("Jane Smith");
        result.HrEmail.Should().Be("hr@acme.co.za");
        result.HrPhone.Should().Be("+27821234567");
        result.Address.Should().Be("123 Main");
        result.City.Should().Be("Cape Town");
        result.Province.Should().Be("Western Cape");
        result.PostalCode.Should().Be("8001");
    }
}
