using EmploymentVerify.Application.Verifications.Commands;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Verifications;

/// <summary>
/// Integration tests for the HR confirmation lifecycle.
/// Uses SQLite (not InMemory) because ExecuteUpdateAsync requires a relational provider.
/// </summary>
public class HrConfirmationLifecycleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RecordHrResponseCommandHandler _handler;

    public HrConfirmationLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _handler = new RecordHrResponseCommandHandler(_context);
    }

    private async Task<(VerificationRequest verification, EmailVerificationToken token)> SeedPendingVerification(
        string tokenValue = "abc123", bool isUsed = false, bool isExpired = false)
    {
        var requestor = new User
        {
            Id = Guid.NewGuid(),
            Email = "r@t.com",
            PasswordHash = "h",
            FullName = "R",
            Role = UserRole.Requestor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(requestor);

        var verification = new VerificationRequest
        {
            Id = Guid.NewGuid(),
            RequestorId = requestor.Id,
            EmployeeFullName = "John Doe",
            IdType = IdentificationType.SaIdNumber,
            SaIdNumber = "8501015026085",
            CompanyName = "Acme Corp",
            JobTitle = "Engineer",
            EmploymentStartDate = new DateOnly(2020, 1, 1),
            PopiaConsentGiven = true,
            AccuracyConfirmed = true,
            ConsentType = ConsentType.RequestorWarranted,
            ConsentRecordedAt = DateTime.UtcNow,
            Status = VerificationStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
        _context.VerificationRequests.Add(verification);

        var token = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            VerificationRequestId = verification.Id,
            Token = tokenValue,
            ExpiresAt = isExpired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(48),
            IsUsed = isUsed,
            CreatedAt = DateTime.UtcNow
        };
        _context.EmailVerificationTokens.Add(token);

        await _context.SaveChangesAsync();
        return (verification, token);
    }

    private static RecordHrResponseCommand BuildResponse(string token, ResponseType responseType = ResponseType.Confirmed) =>
        new(token, "HR Manager", responseType, "Engineer", new DateOnly(2020, 1, 1), null, true, null);

    [Fact]
    public async Task Handle_Valid_Token_Returns_True_And_Updates_Status()
    {
        var (verification, _) = await SeedPendingVerification();
        var result = await _handler.Handle(BuildResponse("abc123"), CancellationToken.None);

        result.Should().BeTrue();

        var updated = await _context.VerificationRequests.FindAsync(verification.Id);
        updated!.Status.Should().Be(VerificationStatus.Confirmed);
        updated.CompletedAt.Should().NotBeNull();
        updated.VerificationMethod.Should().Be(VerificationMethod.Email);
    }

    [Fact]
    public async Task Handle_Denied_Response_Sets_Status_To_Denied()
    {
        var (verification, _) = await SeedPendingVerification();
        var result = await _handler.Handle(BuildResponse("abc123", ResponseType.Denied), CancellationToken.None);

        result.Should().BeTrue();

        var updated = await _context.VerificationRequests.FindAsync(verification.Id);
        updated!.Status.Should().Be(VerificationStatus.Denied);
    }

    [Fact]
    public async Task Handle_Used_Token_Returns_False()
    {
        await SeedPendingVerification(isUsed: true);
        var result = await _handler.Handle(BuildResponse("abc123"), CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Expired_Token_Returns_False()
    {
        await SeedPendingVerification(isExpired: true);
        var result = await _handler.Handle(BuildResponse("abc123"), CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Unknown_Token_Returns_False()
    {
        var result = await _handler.Handle(BuildResponse("no-such-token"), CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Marks_Token_As_Used_After_Confirmation()
    {
        await SeedPendingVerification();
        await _handler.Handle(BuildResponse("abc123"), CancellationToken.None);

        // Reload from DB (ExecuteUpdateAsync bypasses change tracker)
        var token = await _context.EmailVerificationTokens.AsNoTracking().FirstAsync(t => t.Token == "abc123");
        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Prevents_Double_Submission()
    {
        await SeedPendingVerification();
        var first = await _handler.Handle(BuildResponse("abc123"), CancellationToken.None);
        var second = await _handler.Handle(BuildResponse("abc123"), CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
