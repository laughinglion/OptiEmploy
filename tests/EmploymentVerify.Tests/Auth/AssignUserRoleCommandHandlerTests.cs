using EmploymentVerify.Application.Auth.Commands;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using EmploymentVerify.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Tests.Auth;

public class AssignUserRoleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AssignUserRoleCommandHandler _handler;

    public AssignUserRoleCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _handler = new AssignUserRoleCommandHandler(_context);
    }

    [Fact]
    public async Task Handle_Should_Change_User_Role()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = UserRole.Requestor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var command = new AssignUserRoleCommand(userId, UserRole.Operator);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.UserId.Should().Be(userId);
        result.Email.Should().Be("user@example.com");
        result.PreviousRole.Should().Be("Requestor");
        result.NewRole.Should().Be("Operator");

        var user = await _context.Users.FirstAsync(u => u.Id == userId);
        user.Role.Should().Be(UserRole.Operator);
    }

    [Fact]
    public async Task Handle_Should_Change_Role_To_Admin()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = userId,
            Email = "promote@example.com",
            PasswordHash = "hash",
            FullName = "Promote User",
            Role = UserRole.Requestor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var command = new AssignUserRoleCommand(userId, UserRole.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.PreviousRole.Should().Be("Requestor");
        result.NewRole.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_User_Not_Found()
    {
        var command = new AssignUserRoleCommand(Guid.NewGuid(), UserRole.Admin);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task Handle_Should_Persist_Role_Change()
    {
        var userId = Guid.NewGuid();
        _context.Users.Add(new User
        {
            Id = userId,
            Email = "persist@example.com",
            PasswordHash = "hash",
            FullName = "Persist User",
            Role = UserRole.Requestor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _handler.Handle(new AssignUserRoleCommand(userId, UserRole.Operator), CancellationToken.None);

        // Re-query to confirm persistence
        var user = await _context.Users.FirstAsync(u => u.Id == userId);
        user.Role.Should().Be(UserRole.Operator);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
