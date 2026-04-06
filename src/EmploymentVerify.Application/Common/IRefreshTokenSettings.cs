namespace EmploymentVerify.Application.Common;

/// <summary>
/// Exposes refresh token configuration to Application layer handlers
/// without creating a dependency on the Infrastructure assembly.
/// Implemented and registered by Infrastructure via JwtSettings.
/// </summary>
public interface IRefreshTokenSettings
{
    int RefreshTokenExpirationDays { get; }
}
