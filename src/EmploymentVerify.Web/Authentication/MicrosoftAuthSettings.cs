namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Strongly-typed settings for Microsoft OAuth / Entra ID configuration.
/// Bound from Configuration:Authentication:Microsoft section.
/// </summary>
public sealed class MicrosoftAuthSettings
{
    public const string SectionName = "Authentication:Microsoft";

    /// <summary>
    /// Microsoft Entra ID (Azure AD) Application (client) ID.
    /// Found in Azure Portal > App registrations > Overview.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft Entra ID Client Secret value (not the secret ID).
    /// Found in Azure Portal > App registrations > Certificates &amp; secrets.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The tenant ID or "common" / "organizations" / "consumers".
    /// Use a specific tenant GUID for single-tenant, or "common" for multi-tenant.
    /// Default: "common" (allows any Microsoft account).
    /// </summary>
    public string TenantId { get; set; } = "common";

    /// <summary>
    /// The callback path that Microsoft redirects to after authentication.
    /// Must match the Redirect URI configured in Azure Portal.
    /// Default: /signin-microsoft (follows ASP.NET convention).
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-microsoft";

    /// <summary>
    /// The authority URL for Microsoft identity platform.
    /// Computed from TenantId. Typically https://login.microsoftonline.com/{TenantId}/v2.0
    /// </summary>
    public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";

    /// <summary>
    /// Whether Microsoft SSO is enabled (credentials are configured).
    /// </summary>
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && ClientId != "your-microsoft-client-id";
}
