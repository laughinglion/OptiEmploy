using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Companies;

public class UpdateCompanyCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Company SeedCompany(ApplicationDbContext context)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Original Corp",
            RegistrationNumber = "2020/111111/07",
            HrContactName = "Jane",
            HrEmail = "hr@original.co.za",
            HrPhone = "+27821111111",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.SaveChanges();
        return company;
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesCompanyAndReturnsDto()
    {
        // Arrange
        using var context = CreateDbContext();
        var company = SeedCompany(context);
        var handler = new UpdateCompanyCommandHandler(context);

        var command = new UpdateCompanyCommand(
            CompanyId: company.Id,
            Name: "Updated Corp",
            RegistrationNumber: "2020/222222/07",
            HrContactName: "Bob",
            HrEmail: "hr@updated.co.za",
            HrPhone: "+27822222222",
            Address: "456 New St",
            City: "Johannesburg",
            Province: "Gauteng",
            PostalCode: "2000",
            ForceCall: true,
            IsActive: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated Corp");
        result.RegistrationNumber.Should().Be("2020/222222/07");
        result.HrContactName.Should().Be("Bob");
        result.HrEmail.Should().Be("hr@updated.co.za");
        result.ForceCall.Should().BeTrue();
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_CompanyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new UpdateCompanyCommandHandler(context);

        var command = new UpdateCompanyCommand(
            CompanyId: Guid.NewGuid(),
            Name: "X", RegistrationNumber: "2020/111111/07",
            HrContactName: "X", HrEmail: "x@x.co.za", HrPhone: "+27821111111",
            Address: null, City: null, Province: null, PostalCode: null,
            ForceCall: false, IsActive: true);

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_DuplicateRegistrationNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var company1 = SeedCompany(context);
        var company2 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Other Corp",
            RegistrationNumber = "2020/999999/07",
            HrContactName = "Alice",
            HrEmail = "hr@other.co.za",
            HrPhone = "+27829999999",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company2);
        await context.SaveChangesAsync();

        var handler = new UpdateCompanyCommandHandler(context);

        // Try to update company2 with company1's reg number
        var command = new UpdateCompanyCommand(
            CompanyId: company2.Id,
            Name: "Other Corp",
            RegistrationNumber: company1.RegistrationNumber,
            HrContactName: "Alice", HrEmail: "hr@other.co.za", HrPhone: "+27829999999",
            Address: null, City: null, Province: null, PostalCode: null,
            ForceCall: false, IsActive: true);

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }
}
