namespace EmploymentVerify.Infrastructure.Authentication;

/// <summary>
/// Configuration settings for JWT token generation and validation.
/// Bound from appsettings.json section "Jwt".
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Secret key used to sign and verify JWT tokens. Must be at least 32 characters.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Token issuer (typically the application URL).</summary>
    public string Issuer { get; set; } = "EmploymentVerify";

    /// <summary>Token audience (typically the application URL).</summary>
    public string Audience { get; set; } = "EmploymentVerify";

    /// <summary>Token expiration time in minutes.</summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>Refresh token expiration time in days.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
