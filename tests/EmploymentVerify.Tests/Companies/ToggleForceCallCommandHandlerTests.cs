using EmploymentVerify.Application.Companies.Commands;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Companies;

public class ToggleForceCallCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Company SeedCompany(ApplicationDbContext context, bool forceCall = false)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test Corp (Pty) Ltd",
            RegistrationNumber = "2020/123456/07",
            HrContactName = "Jane Smith",
            HrEmail = "hr@testcorp.co.za",
            HrPhone = "+27821234567",
            ForceCall = forceCall,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        context.SaveChanges();
        return company;
    }

    [Fact]
    public async Task Handle_SetForceCallTrue_FlagsCompanyAsForceCallOnly()
    {
        // Arrange
        using var context = CreateDbContext();
        var company = SeedCompany(context, forceCall: false);
        var handler = new ToggleForceCallCommandHandler(context);

        var command = new ToggleForceCallCommand(company.Id, ForceCall: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.CompanyId.Should().Be(company.Id);
        result.CompanyName.Should().Be("Test Corp (Pty) Ltd");
        result.ForceCall.Should().BeTrue();

        // Verify persisted
        var persisted = await context.Companies.FindAsync(company.Id);
        persisted!.ForceCall.Should().BeTrue();
        persisted.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_SetForceCallFalse_UnflagsCompany()
    {
        // Arrange
        using var context = CreateDbContext();
        var company = SeedCompany(context, forceCall: true);
        var handler = new ToggleForceCallCommandHandler(context);

        var command = new ToggleForceCallCommand(company.Id, ForceCall: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.ForceCall.Should().BeFalse();

        var persisted = await context.Companies.FindAsync(company.Id);
        persisted!.ForceCall.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CompanyNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext();
        var handler = new ToggleForceCallCommandHandler(context);

        var command = new ToggleForceCallCommand(Guid.NewGuid(), ForceCall: true);

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_SetsUpdatedAtTimestamp()
    {
        // Arrange
        using var context = CreateDbContext();
        var company = SeedCompany(context, forceCall: false);
        var handler = new ToggleForceCallCommandHandler(context);

        var before = DateTime.UtcNow.AddSeconds(-1);
        var command = new ToggleForceCallCommand(company.Id, ForceCall: true);

        // Act
        await handler.Handle(command, CancellationToken.None);
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var persisted = await context.Companies.FindAsync(company.Id);
        persisted!.UpdatedAt.Should().NotBeNull();
        persisted.UpdatedAt!.Value.Should().BeAfter(before).And.BeBefore(after);
    }

    [Fact]
    public async Task Handle_IdempotentWhenAlreadyFlagged_StillSucceeds()
    {
        // Arrange - company already has ForceCall = true
        using var context = CreateDbContext();
        var company = SeedCompany(context, forceCall: true);
        var handler = new ToggleForceCallCommandHandler(context);

        var command = new ToggleForceCallCommand(company.Id, ForceCall: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - should succeed without error
        result.ForceCall.Should().BeTrue();
    }
}
