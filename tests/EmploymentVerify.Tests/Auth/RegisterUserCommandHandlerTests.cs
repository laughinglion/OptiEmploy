using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Auth;

public class RegisterUserCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RegisterUserCommandHandler _handler;
    private readonly FakePasswordHasher _passwordHasher;
    private readonly FakeEmailSender _emailSender;
    private readonly FakeTokenGenerator _tokenGenerator;

    public RegisterUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _passwordHasher = new FakePasswordHasher();
        _emailSender = new FakeEmailSender();
        _tokenGenerator = new FakeTokenGenerator();
        _handler = new RegisterUserCommandHandler(_context, _passwordHasher, _emailSender, _tokenGenerator);
    }

    [Fact]
    public async Task Handle_Should_Create_User_With_Hashed_Password()
    {
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecureP@ss1",
            FullName: "John Doe",
            CompanyName: "Acme Corp");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("John Doe");
        result.Role.Should().Be("Requestor");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.UserId);
        user.Should().NotBeNull();
        user!.PasswordHash.Should().Be("HASHED:SecureP@ss1");
        user.Role.Should().Be(UserRole.Requestor);
        user.CreditBalance.Should().Be(0m);
        user.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task Handle_Should_Set_User_Inactive_Until_Email_Verified()
    {
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecureP@ss1",
            FullName: "John Doe",
            CompanyName: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.UserId);
        user.Should().NotBeNull();
        user!.IsActive.Should().BeFalse();
        user.IsEmailVerified.Should().BeFalse();
        user.EmailVerificationToken.Should().NotBeNullOrEmpty();
        user.EmailVerificationTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_Should_Send_Verification_Email()
    {
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecureP@ss1",
            FullName: "John Doe",
            CompanyName: null);

        await _handler.Handle(command, CancellationToken.None);

        _emailSender.SentEmails.Should().HaveCount(1);
        var sentEmail = _emailSender.SentEmails[0];
        sentEmail.To.Should().Be("test@example.com");
        sentEmail.Subject.Should().Contain("Verify your email");
        sentEmail.HtmlBody.Should().Contain("John Doe");
        sentEmail.HtmlBody.Should().Contain("POPIA");
    }

    [Fact]
    public async Task Handle_Should_Return_EmailVerificationRequired_True()
    {
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecureP@ss1",
            FullName: "John Doe",
            CompanyName: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.EmailVerificationRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Default_To_Requestor_When_No_Role_Specified()
    {
        var command = new RegisterUserCommand(
            Email: "norole@example.com",
            Password: "SecureP@ss1",
            FullName: "No Role User",
            CompanyName: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Role.Should().Be("Requestor");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.UserId);
        user!.Role.Should().Be(UserRole.Requestor);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Operator)]
    [InlineData(UserRole.Requestor)]
    public async Task Handle_Should_Assign_Specified_Role(UserRole role)
    {
        var command = new RegisterUserCommand(
            Email: $"{role.ToString().ToLower()}@example.com",
            Password: "SecureP@ss1",
            FullName: "Role Test User",
            CompanyName: null,
            Role: role);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Role.Should().Be(role.ToString());
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.UserId);
        user!.Role.Should().Be(role);
    }

    [Fact]
    public async Task Handle_Should_Normalize_Email_To_Lowercase()
    {
        var command = new RegisterUserCommand(
            Email: "  Test@EXAMPLE.com  ",
            Password: "SecureP@ss1",
            FullName: "John Doe",
            CompanyName: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Handle_Should_Trim_FullName()
    {
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecureP@ss1",
            FullName: "  John Doe  ",
            CompanyName: null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Email_Already_Exists()
    {
        _context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            PasswordHash = "hash",
            FullName = "Existing User",
            Role = UserRole.Requestor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var command = new RegisterUserCommand(
            Email: "existing@example.com",
            Password: "SecureP@ss1",
            FullName: "New User",
            CompanyName: null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_Should_Store_User_In_Database()
    {
        var command = new RegisterUserCommand(
            Email: "stored@example.com",
            Password: "SecureP@ss1",
            FullName: "Stored User",
            CompanyName: "TestCo");

        await _handler.Handle(command, CancellationToken.None);

        var count = await _context.Users.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Generate_Token_With_24h_Expiry()
    {
        var command = new RegisterUserCommand(
            Email: "test@example.com",
            Password: "SecureP@ss1",
            FullName: "Test User",
            CompanyName: null);

        var beforeUtc = DateTime.UtcNow;
        await _handler.Handle(command, CancellationToken.None);
        var afterUtc = DateTime.UtcNow;

        var user = await _context.Users.FirstAsync();
        user.EmailVerificationToken.Should().Be("test-token-12345");
        user.EmailVerificationTokenExpiresAt.Should().BeOnOrAfter(beforeUtc.AddHours(24));
        user.EmailVerificationTokenExpiresAt.Should().BeOnOrBefore(afterUtc.AddHours(24));
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"HASHED:{password}";
        public bool Verify(string password, string hash) => hash == $"HASHED:{password}";
    }

    private class FakeEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string HtmlBody)> SentEmails { get; } = new();

        public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            SentEmails.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private class FakeTokenGenerator : IEmailVerificationTokenGenerator
    {
        public string GenerateToken() => "test-token-12345";
    }
}
