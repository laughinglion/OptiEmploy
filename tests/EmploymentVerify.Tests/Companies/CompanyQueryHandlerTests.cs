using EmploymentVerify.Application.Companies.Queries;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Companies;

public class CompanyQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedCompanies(ApplicationDbContext context)
    {
        context.Companies.AddRange(
            new Company
            {
                Id = Guid.NewGuid(), Name = "Alpha Corp", RegistrationNumber = "2020/000001/07",
                HrContactName = "A", HrEmail = "hr@alpha.co.za", HrPhone = "+27821000001",
                City = "Cape Town", IsActive = true, IsVerified = true, CreatedAt = DateTime.UtcNow
            },
            new Company
            {
                Id = Guid.NewGuid(), Name = "Beta Inc", RegistrationNumber = "2020/000002/07",
                HrContactName = "B", HrEmail = "hr@beta.co.za", HrPhone = "+27821000002",
                City = "Johannesburg", IsActive = true, IsVerified = false, CreatedAt = DateTime.UtcNow
            },
            new Company
            {
                Id = Guid.NewGuid(), Name = "Gamma Ltd", RegistrationNumber = "2020/000003/07",
                HrContactName = "C", HrEmail = "hr@gamma.co.za", HrPhone = "+27821000003",
                City = "Durban", IsActive = false, IsVerified = true, CreatedAt = DateTime.UtcNow
            }
        );
        context.SaveChanges();
    }

    [Fact]
    public async Task GetById_ExistingCompany_ReturnsDto()
    {
        // Arrange
        using var context = CreateDbContext();
        var company = new Company
        {
            Id = Guid.NewGuid(), Name = "Test Corp", RegistrationNumber = "2020/999999/07",
            HrContactName = "Jane", HrEmail = "hr@test.co.za", HrPhone = "+27821234567",
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var handler = new GetCompanyByIdQueryHandler(context);

        // Act
        var result = await handler.Handle(new GetCompanyByIdQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(company.Id);
        result.Name.Should().Be("Test Corp");
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        using var context = CreateDbContext();
        var handler = new GetCompanyByIdQueryHandler(context);

        var result = await handler.Handle(new GetCompanyByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListCompanies_DefaultFilters_ReturnsOnlyActive()
    {
        // Arrange
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new ListCompaniesQueryHandler(context);

        // Act
        var result = await handler.Handle(new ListCompaniesQuery(), CancellationToken.None);

        // Assert — Gamma is inactive, should be excluded
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task ListCompanies_IncludeInactive_ReturnsAll()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new ListCompaniesQueryHandler(context);

        var result = await handler.Handle(new ListCompaniesQuery(IncludeInactive: true), CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ListCompanies_SearchByName_ReturnsMatching()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new ListCompaniesQueryHandler(context);

        var result = await handler.Handle(
            new ListCompaniesQuery(SearchTerm: "alpha", IncludeInactive: true),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Alpha Corp");
    }

    [Fact]
    public async Task ListCompanies_SearchByRegistrationNumber_ReturnsMatching()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new ListCompaniesQueryHandler(context);

        var result = await handler.Handle(
            new ListCompaniesQuery(SearchTerm: "000002", IncludeInactive: true),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Beta Inc");
    }

    [Fact]
    public async Task ListCompanies_OrderedByName()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new ListCompaniesQueryHandler(context);

        var result = await handler.Handle(new ListCompaniesQuery(IncludeInactive: true), CancellationToken.None);

        result.Select(c => c.Name).Should().BeInAscendingOrder();
    }
}
