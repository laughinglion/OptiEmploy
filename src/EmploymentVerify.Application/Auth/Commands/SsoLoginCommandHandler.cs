using EmploymentVerify.Application.Common;
using EmploymentVerify.Domain.Entities;
using EmploymentVerify.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EmploymentVerify.Application.Auth.Commands;

public class SsoLoginCommandHandler : IRequestHandler<SsoLoginCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly int _refreshTokenDays;

    public SsoLoginCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenSettings refreshTokenSettings)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenDays = refreshTokenSettings.RefreshTokenExpirationDays;
    }

    public async Task<LoginResult> Handle(SsoLoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            // First-time SSO login — create account automatically
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = string.Empty, // SSO users have no password
                FullName = request.FullName.Trim(),
                Role = UserRole.Requestor,
                CreditBalance = 0m,
                IsActive = true,
                IsEmailVerified = true, // Provider has already verified the email
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else if (!user.IsActive)
        {
            return new LoginResult(false, null, null, null, null, null, null,
                "Your account has been deactivated. Please contact support.");
        }

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
