using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Auth;

public class LoginCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LoginCommandHandler _handler;
    private readonly FakePasswordHasher _hasher;
    private readonly FakeJwtGenerator _jwtGenerator;
    private readonly FakeRefreshTokenSettings _settings;

    public LoginCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _hasher = new FakePasswordHasher();
        _jwtGenerator = new FakeJwtGenerator();
        _settings = new FakeRefreshTokenSettings();
        _handler = new LoginCommandHandler(_context, _hasher, _jwtGenerator, _settings);
    }

    private async Task<User> CreateActiveUser(string email = "user@test.com", string password = "password")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(password),
            FullName = "Test User",
            Role = UserRole.Requestor,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_Valid_Credentials_Returns_Success()
    {
        await CreateActiveUser();
        var result = await _handler.Handle(new LoginCommand("user@test.com", "password"), CancellationToken.None);
        result.Success.Should().BeTrue();
        result.Token.Should().Be("jwt-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Wrong_Password_Returns_Failure()
    {
        await CreateActiveUser();
        var result = await _handler.Handle(new LoginCommand("user@test.com", "wrong"), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_Unknown_Email_Returns_Failure()
    {
        var result = await _handler.Handle(new LoginCommand("nobody@test.com", "password"), CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Inactive_User_Returns_Failure()
    {
        var user = await CreateActiveUser();
        user.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new LoginCommand("user@test.com", "password"), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task Handle_Increments_FailedAttempts_On_Wrong_Password()
    {
        var user = await CreateActiveUser();
        await _handler.Handle(new LoginCommand("user@test.com", "wrong"), CancellationToken.None);

        var updated = await _context.Users.FindAsync(user.Id);
        updated!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Locks_Account_After_5_Failed_Attempts()
    {
        await CreateActiveUser();
        for (int i = 0; i < 5; i++)
            await _handler.Handle(new LoginCommand("user@test.com", "wrong"), CancellationToken.None);

        var result = await _handler.Handle(new LoginCommand("user@test.com", "wrong"), CancellationToken.None);
        result.ErrorMessage.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_Resets_FailedAttempts_On_Success()
    {
        var user = await CreateActiveUser();
        user.FailedLoginAttempts = 3;
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new LoginCommand("user@test.com", "password"), CancellationToken.None);

        result.Success.Should().BeTrue();
        var updated = await _context.Users.FindAsync(user.Id);
        updated!.FailedLoginAttempts.Should().Be(0);
        updated.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Returns_Failure_When_Account_Locked()
    {
        var user = await CreateActiveUser();
        user.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new LoginCommand("user@test.com", "password"), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_Persists_RefreshToken_In_Database()
    {
        await CreateActiveUser();
        await _handler.Handle(new LoginCommand("user@test.com", "password"), CancellationToken.None);

        var token = await _context.RefreshTokens.FirstOrDefaultAsync();
        token.Should().NotBeNull();
        token!.IsRevoked.Should().BeFalse();
        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    public void Dispose() => _context.Dispose();

    private class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string p) => $"H:{p}";
        public bool Verify(string p, string h) => h == $"H:{p}";
    }

    private class FakeJwtGenerator : IJwtTokenGenerator
    {
        public string GenerateToken(Domain.Entities.User user) => "jwt-token";
    }

    private class FakeRefreshTokenSettings : IRefreshTokenSettings
    {
        public int RefreshTokenExpirationDays => 7;
    }
}
