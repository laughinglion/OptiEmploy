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
/// Tests that concurrent verification submissions cannot double-spend credits.
/// Uses SQLite because ExecuteUpdateAsync requires a relational provider.
/// </summary>
public class CreditDeductionConcurrencyTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly Guid _userId = Guid.NewGuid();

    public CreditDeductionConcurrencyTests()
    {
        // Use a file-based SQLite DB so multiple DbContext instances share state
        _dbPath = Path.Combine(Path.GetTempPath(), $"credit_test_{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"DataSource={_dbPath}")
            .Options;

        using var ctx = new ApplicationDbContext(_options);
        ctx.Database.EnsureCreated();

        // Seed a requestor with exactly 1 credit
        ctx.Users.Add(new User
        {
            Id = _userId,
            Email = "concurrent@test.com",
            PasswordHash = "h",
            FullName = "Concurrent Test",
            Role = UserRole.Requestor,
            CreditBalance = 1m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Concurrent_Submissions_With_One_Credit_Only_One_Succeeds()
    {
        var pricing = Options.Create(new PricingSettings { VerificationCostCredits = 1.00m });
        var exceptions = new List<Exception>();
        var successes = 0;

        // Launch two concurrent submissions using separate DbContext instances
        var tasks = Enumerable.Range(0, 2).Select(async i =>
        {
            await using var ctx = new ApplicationDbContext(_options);
            var handler = new SubmitVerificationCommandHandler(ctx, new NullMediator(), pricing);
            var command = BuildCommand(_userId);

            try
            {
                await handler.Handle(command, CancellationToken.None);
                Interlocked.Increment(ref successes);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient"))
            {
                lock (exceptions) { exceptions.Add(ex); }
            }
        });

        await Task.WhenAll(tasks);

        // Exactly one should succeed, one should fail with insufficient credits
        successes.Should().Be(1, "only one submission should succeed with 1 credit");
        exceptions.Should().HaveCount(1, "the other should fail with insufficient credits");

        // Verify final balance is 0, not negative
        await using var verifyCtx = new ApplicationDbContext(_options);
        var user = await verifyCtx.Users.AsNoTracking().FirstAsync(u => u.Id == _userId);
        user.CreditBalance.Should().Be(0m, "balance should never go negative");
    }

    public void Dispose()
    {
        try { File.Delete(_dbPath); } catch { /* cleanup best-effort */ }
    }

    private static SubmitVerificationCommand BuildCommand(Guid userId) =>
        new(userId,
            "Jane Smith",
            "SaIdNumber",
            "8501015026085",
            null, null,
            "Acme Corp",
            null,
            "Software Engineer",
            new DateOnly(2020, 1, 1),
            null,
            "HR Manager",
            "hr@acme.com",
            "+27821234567",
            true, true,
            "RequestorWarranted",
            "https://app.test");

    private sealed class NullMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
            => Task.FromResult(default(TResponse)!);
        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest
            => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken ct = default)
            => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification
            => Task.CompletedTask;
        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        { await Task.CompletedTask; yield break; }
        public async IAsyncEnumerable<object?> CreateStream(object request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        { await Task.CompletedTask; yield break; }
    }
}
