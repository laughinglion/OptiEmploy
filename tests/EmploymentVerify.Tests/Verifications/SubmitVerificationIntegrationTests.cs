using EmploymentVerify.Application.Common;
using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Domain.Constants;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmploymentVerify.Tests.Verifications;

/// <summary>
/// Integration tests for verification submission: covers credit deduction, routing, and POPIA consent recording.
/// Uses an in-memory database with a fake mediator (to avoid sending real emails).
/// </summary>
public class SubmitVerificationIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SubmitVerificationCommandHandler _handler;
    private readonly FakeMediator _mediator;

    public SubmitVerificationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _mediator = new FakeMediator();
        var pricing = Options.Create(new PricingSettings { VerificationCostCredits = 1.00m });
        _handler = new SubmitVerificationCommandHandler(_context, _mediator, pricing);
    }

    private async Task<User> CreateRequestorWithCredits(decimal credits = 5m)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "requestor@test.com",
            PasswordHash = "h",
            FullName = "Test Requestor",
            Role = UserRole.Requestor,
            CreditBalance = credits,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private static SubmitVerificationCommand BuildCommand(Guid userId, Guid? companyId = null) =>
        new(userId,
            "Jane Smith",
            "SaIdNumber",
            "8501015026085",
            null, null,
            "Acme Corp",
            companyId,
            "Software Engineer",
            new DateOnly(2020, 1, 1),
            null,
            "HR Manager",
            "hr@acme.com",
            "+27821234567",
            true, true,
            "RequestorWarranted",
            "https://app.test");

    [Fact]
    public async Task Handle_Deducts_One_Credit_From_Requestor()
    {
        var user = await CreateRequestorWithCredits(5m);
        await _handler.Handle(BuildCommand(user.Id), CancellationToken.None);

        // Re-read with AsNoTracking — ExecuteUpdateAsync bypasses the change tracker
        var updated = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        updated.CreditBalance.Should().Be(4m);
    }

    [Fact]
    public async Task Handle_Records_Credit_Transaction()
    {
        var user = await CreateRequestorWithCredits(5m);
        await _handler.Handle(BuildCommand(user.Id), CancellationToken.None);

        var tx = await _context.CreditTransactions.AsNoTracking().FirstOrDefaultAsync();
        tx.Should().NotBeNull();
        tx!.Amount.Should().Be(-1m);
        tx.BalanceBefore.Should().Be(5m);
        tx.BalanceAfter.Should().Be(4m);
        tx.TransactionType.Should().Be("Debit");
    }

    [Fact]
    public async Task Handle_Throws_When_Insufficient_Credits()
    {
        var user = await CreateRequestorWithCredits(0m);
        var act = () => _handler.Handle(BuildCommand(user.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Insufficient*");
    }

    [Fact]
    public async Task Handle_Creates_Verification_With_Pending_Status()
    {
        var user = await CreateRequestorWithCredits(5m);
        var result = await _handler.Handle(BuildCommand(user.Id), CancellationToken.None);

        result.Status.Should().Be("Pending");
        var verification = await _context.VerificationRequests.FindAsync(result.Id);
        verification.Should().NotBeNull();
        verification!.PopiaConsentGiven.Should().BeTrue();
        verification.AccuracyConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Routes_To_Operator_When_Company_ForceCall()
    {
        var user = await CreateRequestorWithCredits(5m);
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "ForceCall Corp",
            RegistrationNumber = "FC001",
            HrContactName = "HR",
            HrEmail = "hr@fc.com",
            HrPhone = "+271",
            ForceCall = true,
            IsVerified = true,
            IsActive = true
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        await _handler.Handle(BuildCommand(user.Id, company.Id), CancellationToken.None);

        // ForceCall=true → no email sent
        _mediator.EmailsSent.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Sends_Email_When_Company_In_Directory_Without_ForceCall()
    {
        var user = await CreateRequestorWithCredits(5m);
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Email Corp",
            RegistrationNumber = "EC001",
            HrContactName = "HR",
            HrEmail = "hr@email.com",
            HrPhone = "+272",
            ForceCall = false,
            IsVerified = true,
            IsActive = true
        };
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        await _handler.Handle(BuildCommand(user.Id, company.Id), CancellationToken.None);

        _mediator.EmailsSent.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    private class FakeMediator : IMediator
    {
        public int EmailsSent { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is SendVerificationEmailCommand)
            {
                EmailsSent++;
                return Task.FromResult((TResponse)(object)true);
            }
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => Task.CompletedTask;

        public async IAsyncEnumerable<object?> CreateStream(object request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(null);
    }
}
