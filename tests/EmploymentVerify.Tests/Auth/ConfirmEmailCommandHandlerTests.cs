using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Auth;

public class ConfirmEmailCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ConfirmEmailCommandHandler _handler;

    public ConfirmEmailCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _handler = new ConfirmEmailCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_Should_Activate_User_With_Valid_Token()
    {
        var user = CreateUnverifiedUser("valid-token", DateTime.UtcNow.AddHours(24));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ConfirmEmailCommand("valid-token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.Message.Should().Contain("verified successfully");

        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.IsEmailVerified.Should().BeTrue();
        updatedUser.IsActive.Should().BeTrue();
        updatedUser.EmailVerifiedAt.Should().NotBeNull();
        updatedUser.EmailVerificationToken.Should().BeNull();
        updatedUser.EmailVerificationTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Fail_With_Invalid_Token()
    {
        var command = new ConfirmEmailCommand("nonexistent-token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid verification token");
    }

    [Fact]
    public async Task Handle_Should_Fail_With_Empty_Token()
    {
        var command = new ConfirmEmailCommand("");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("required");
    }

    [Fact]
    public async Task Handle_Should_Fail_With_Whitespace_Token()
    {
        var command = new ConfirmEmailCommand("   ");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("required");
    }

    [Fact]
    public async Task Handle_Should_Fail_With_Expired_Token()
    {
        var user = CreateUnverifiedUser("expired-token", DateTime.UtcNow.AddHours(-1));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ConfirmEmailCommand("expired-token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired");

        var unchangedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        unchangedUser.IsEmailVerified.Should().BeFalse();
        unchangedUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Email_Already_Verified()
    {
        var user = CreateUnverifiedUser("already-verified-token", DateTime.UtcNow.AddHours(24));
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow.AddHours(-1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ConfirmEmailCommand("already-verified-token");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already been verified");
    }

    [Fact]
    public async Task Handle_Should_Set_EmailVerifiedAt_Timestamp()
    {
        var user = CreateUnverifiedUser("timestamp-token", DateTime.UtcNow.AddHours(24));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var beforeUtc = DateTime.UtcNow;
        var command = new ConfirmEmailCommand("timestamp-token");
        await _handler.Handle(command, CancellationToken.None);
        var afterUtc = DateTime.UtcNow;

        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.EmailVerifiedAt.Should().BeOnOrAfter(beforeUtc);
        updatedUser.EmailVerifiedAt.Should().BeOnOrBefore(afterUtc);
    }

    [Fact]
    public async Task Handle_Should_Clear_Token_After_Verification()
    {
        var user = CreateUnverifiedUser("clear-token", DateTime.UtcNow.AddHours(24));
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ConfirmEmailCommand("clear-token");
        await _handler.Handle(command, CancellationToken.None);

        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.EmailVerificationToken.Should().BeNull();
        updatedUser.EmailVerificationTokenExpiresAt.Should().BeNull();
    }

    private static User CreateUnverifiedUser(string token, DateTime tokenExpiry)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = $"user-{Guid.NewGuid():N}@example.com",
            PasswordHash = "hashed-password",
            FullName = "Test User",
            Role = UserRole.Requestor,
            IsActive = false,
            IsEmailVerified = false,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiresAt = tokenExpiry,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
