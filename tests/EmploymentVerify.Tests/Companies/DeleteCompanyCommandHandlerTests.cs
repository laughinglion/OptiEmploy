using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Companies;

public class DeleteCompanyCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ExistingCompany_SoftDeletesSetsInactive()
    {
        // Arrange
        using var context = CreateDbContext();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test Corp",
            RegistrationNumber = "2020/123456/07",
            HrContactName = "Jane",
            HrEmail = "hr@test.co.za",
            HrPhone = "+27821234567",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new DeleteCompanyCommandHandler(context);

        // Act
        await handler.Handle(new DeleteCompanyCommand(company.Id), CancellationToken.None);

        // Assert — soft delete: data preserved, IsActive = false
        var deleted = await context.Companies.FirstAsync(c => c.Id == company.Id);
        deleted.IsActive.Should().BeFalse();
        deleted.UpdatedAt.Should().NotBeNull();
        deleted.Name.Should().Be("Test Corp"); // data preserved for POPIA
    }

    [Fact]
    public async Task Handle_CompanyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new DeleteCompanyCommandHandler(context);

        // Act & Assert
        var act = () => handler.Handle(new DeleteCompanyCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }
}
