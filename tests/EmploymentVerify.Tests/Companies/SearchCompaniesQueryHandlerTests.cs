using EmploymentVerify.Application.Companies.Queries;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EmploymentVerify.Tests.Companies;

public class SearchCompaniesQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    private static void SeedCompanies(ApplicationDbContext context)
    {
        context.Companies.AddRange(
            new Company
            {
                Id = Guid.NewGuid(), Name = "Alpha Corp", RegistrationNumber = "2020/000001/07",
                HrContactName = "Anna Alpha", HrEmail = "hr@alpha.co.za", HrPhone = "+27821000001",
                IsActive = true, IsVerified = true, CreatedAt = DateTime.UtcNow
            },
            new Company
            {
                Id = Guid.NewGuid(), Name = "Alpha Holdings", RegistrationNumber = "2020/000004/07",
                HrContactName = "Bob Alpha", HrEmail = "hr@alphaholdings.co.za", HrPhone = "+27821000004",
                IsActive = true, IsVerified = true, CreatedAt = DateTime.UtcNow
            },
            new Company
            {
                Id = Guid.NewGuid(), Name = "Beta Inc", RegistrationNumber = "2020/000002/07",
                HrContactName = "Beth Beta", HrEmail = "hr@beta.co.za", HrPhone = "+27821000002",
                IsActive = true, IsVerified = false, CreatedAt = DateTime.UtcNow
            },
            new Company
            {
                Id = Guid.NewGuid(), Name = "Gamma Ltd", RegistrationNumber = "2020/000003/07",
                HrContactName = "Gary Gamma", HrEmail = "hr@gamma.co.za", HrPhone = "+27821000003",
                IsActive = false, IsVerified = true, CreatedAt = DateTime.UtcNow
            }
        );
        context.SaveChanges();
    }

    [Fact]
    public async Task Search_ReturnsOnlyActiveAndVerifiedCompanies()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("alpha"), CancellationToken.None);

        // Alpha Corp and Alpha Holdings are active+verified; Beta is not verified; Gamma is not active
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.Name.Contains("Alpha"));
    }

    [Fact]
    public async Task Search_ExcludesUnverifiedCompanies()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("beta"), CancellationToken.None);

        // Beta Inc is active but NOT verified
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ExcludesInactiveCompanies()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("gamma"), CancellationToken.None);

        // Gamma Ltd is verified but NOT active
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("ALPHA"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Search_ReturnsEmptyForShortSearchTerm()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("a"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ReturnsEmptyForNullOrWhitespace()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var resultNull = await handler.Handle(new SearchCompaniesQuery(""), CancellationToken.None);
        var resultWhitespace = await handler.Handle(new SearchCompaniesQuery("   "), CancellationToken.None);

        resultNull.Should().BeEmpty();
        resultWhitespace.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_ReturnsHrContactDetails()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("Alpha Corp"), CancellationToken.None);

        result.Should().ContainSingle(c => c.Name == "Alpha Corp");
        var company = result.First(c => c.Name == "Alpha Corp");
        company.HrContactName.Should().Be("Anna Alpha");
        company.HrEmail.Should().Be("hr@alpha.co.za");
        company.HrPhone.Should().Be("+27821000001");
    }

    [Fact]
    public async Task Search_ResultsOrderedByName()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("alpha"), CancellationToken.None);

        result.Select(c => c.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Search_LimitsToTenResults()
    {
        using var context = CreateDbContext();
        // Seed 15 active+verified companies matching "Test"
        for (int i = 0; i < 15; i++)
        {
            context.Companies.Add(new Company
            {
                Id = Guid.NewGuid(),
                Name = $"Test Company {i:D2}",
                RegistrationNumber = $"2020/{i:D6}/07",
                HrContactName = "HR",
                HrEmail = $"hr{i}@test.co.za",
                HrPhone = "+27821000000",
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var handler = new SearchCompaniesQueryHandler(context, CreateCache());
        var result = await handler.Handle(new SearchCompaniesQuery("Test"), CancellationToken.None);

        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmpty()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var handler = new SearchCompaniesQueryHandler(context, CreateCache());

        var result = await handler.Handle(new SearchCompaniesQuery("NonExistent"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_CachesResults_OnSecondCall()
    {
        using var context = CreateDbContext();
        SeedCompanies(context);
        var cache = CreateCache();
        var handler = new SearchCompaniesQueryHandler(context, cache);

        var first = await handler.Handle(new SearchCompaniesQuery("alpha"), CancellationToken.None);
        // Remove companies from DB to prove second call uses cache
        context.Companies.RemoveRange(context.Companies);
        await context.SaveChangesAsync();
        var second = await handler.Handle(new SearchCompaniesQuery("alpha"), CancellationToken.None);

        first.Should().HaveCount(2);
        second.Should().HaveCount(2); // served from cache
    }
}
