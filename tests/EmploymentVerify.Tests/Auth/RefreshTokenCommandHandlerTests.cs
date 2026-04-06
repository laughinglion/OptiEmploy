using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Auth;

public class RefreshTokenCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RefreshTokenCommandHandler _handler;
    private readonly FakeJwtGenerator _jwt;

    public RefreshTokenCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _jwt = new FakeJwtGenerator();
        _handler = new RefreshTokenCommandHandler(_context, _jwt);
    }

    private async Task<(User user, RefreshToken token)> Seed(bool isRevoked = false, bool isExpired = false)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@t.com",
            PasswordHash = "h",
            FullName = "U",
            Role = UserRole.Requestor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid-refresh-token",
            ExpiresAt = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();
        return (user, token);
    }

    [Fact]
    public async Task Handle_Valid_Token_Returns_New_Tokens()
    {
        await Seed();
        var result = await _handler.Handle(new RefreshTokenCommand("valid-refresh-token"), CancellationToken.None);
        result.Success.Should().BeTrue();
        result.Token.Should().Be("jwt-token");
        result.RefreshToken.Should().NotBe("valid-refresh-token");
    }

    [Fact]
    public async Task Handle_Valid_Token_Revokes_Old_Token()
    {
        await Seed();
        await _handler.Handle(new RefreshTokenCommand("valid-refresh-token"), CancellationToken.None);

        var old = await _context.RefreshTokens.FirstAsync(t => t.Token == "valid-refresh-token");
        old.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Revoked_Token_Returns_Failure()
    {
        await Seed(isRevoked: true);
        var result = await _handler.Handle(new RefreshTokenCommand("valid-refresh-token"), CancellationToken.None);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_Expired_Token_Returns_Failure()
    {
        await Seed(isExpired: true);
        var result = await _handler.Handle(new RefreshTokenCommand("valid-refresh-token"), CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Unknown_Token_Returns_Failure()
    {
        var result = await _handler.Handle(new RefreshTokenCommand("does-not-exist"), CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();

    private class FakeJwtGenerator : IJwtTokenGenerator
    {
        public string GenerateToken(User u) => "jwt-token";
    }

}
