using EmploymentVerify.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmploymentVerify.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            return new LoginResult(false, null, null, null, null, null, null, "Invalid or expired refresh token.");

        if (refreshToken.User is null || !refreshToken.User.IsActive)
            return new LoginResult(false, null, null, null, null, null, null, "User account is inactive.");

        // Rotate: revoke old, issue new
        refreshToken.IsRevoked = true;

        var newRefreshTokenValue = System.Security.Cryptography.RandomNumberGenerator.GetBytes(64);
        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = refreshToken.UserId,
            Token = Convert.ToBase64String(newRefreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(refreshToken.User);
        return new LoginResult(true, accessToken, newRefreshToken.Token, refreshToken.User.Id,
            refreshToken.User.Email, refreshToken.User.FullName, refreshToken.User.Role.ToString(), null);
    }
}
