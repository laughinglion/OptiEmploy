using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EmploymentVerify.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly int _refreshTokenDays;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenSettings refreshTokenSettings)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenDays = refreshTokenSettings.RefreshTokenExpirationDays;
    }

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return new LoginResult(false, null, null, null, null, null, null, "Invalid email or password.");

        if (!user.IsActive)
            return new LoginResult(false, null, null, null, null, null, null, "Your account is inactive. Please verify your email or contact support.");

        // Check lockout
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            var remaining = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes + 1;
            return new LoginResult(false, null, null, null, null, null, null,
                $"Account temporarily locked. Try again in {remaining} minute(s).");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }
            await _context.SaveChangesAsync(cancellationToken);
            return new LoginResult(false, null, null, null, null, null, null, "Invalid email or password.");
        }

        // Successful login — reset lockout counters
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var refreshTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResult(true, accessToken, refreshTokenValue, user.Id, user.Email, user.FullName, user.Role.ToString(), null);
    }
}
