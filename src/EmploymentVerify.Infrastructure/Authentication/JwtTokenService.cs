using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmploymentVerify.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmploymentVerify.Infrastructure.Authentication;

/// <summary>
/// Service for generating and validating JWT tokens for API authentication.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Generate a JWT access token for the specified user.</summary>
    string GenerateAccessToken(User user);

    /// <summary>Validate a JWT token and return the claims principal, or null if invalid.</summary>
    ClaimsPrincipal? ValidateToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;

        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("sub", user.Id.ToString()),
            new("email_verified", user.IsEmailVerified.ToString().ToLowerInvariant())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
