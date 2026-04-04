namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Strongly-typed settings for Google OAuth configuration.
/// Bound from Configuration:Authentication:Google section.
/// </summary>
public sealed class GoogleAuthSettings
{
    public const string SectionName = "Authentication:Google";

    /// <summary>
    /// Google OAuth 2.0 Client ID from Google Cloud Console.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Google OAuth 2.0 Client Secret from Google Cloud Console.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The callback path that Google redirects to after authentication.
    /// Default: /signin-google (standard ASP.NET convention).
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-google";

    /// <summary>
    /// Whether Google SSO is enabled (credentials are configured).
    /// </summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && ClientId != "your-google-client-id.apps.googleusercontent.com";
}
