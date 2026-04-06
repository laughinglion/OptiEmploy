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

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
            return new LoginResult(false, null, null, null, null, null, null, "Invalid email or password.");

        if (!user.IsActive)
            return new LoginResult(false, null, null, null, null, null, null, "Your account is inactive. Please verify your email or contact support.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return new LoginResult(false, null, null, null, null, null, null, "Invalid email or password.");

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
