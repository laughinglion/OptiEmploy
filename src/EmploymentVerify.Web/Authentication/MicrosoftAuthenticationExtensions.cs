using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Extension methods to configure Microsoft Entra ID (Azure AD) OAuth SSO.
/// Uses OpenID Connect protocol for Microsoft identity platform v2.0.
/// </summary>
public static class MicrosoftAuthenticationExtensions
{
    /// <summary>
    /// The authentication scheme name for Microsoft SSO.
    /// </summary>
    public const string MicrosoftScheme = "Microsoft";

    /// <summary>
    /// Adds Microsoft Entra ID OpenID Connect authentication to the authentication builder.
    /// Must be called after cookie authentication has been registered.
    /// Client credentials are read from Configuration:Authentication:Microsoft.
    /// </summary>
    public static AuthenticationBuilder AddMicrosoftSsoAuthentication(
        this AuthenticationBuilder builder,
        IConfiguration configuration)
    {
        var msSection = configuration.GetSection(MicrosoftAuthSettings.SectionName);
        var clientId = msSection["ClientId"];
        var clientSecret = msSection["ClientSecret"];
        var tenantId = msSection["TenantId"] ?? "common";
        var callbackPath = msSection["CallbackPath"] ?? "/signin-microsoft";

        builder.AddOpenIdConnect(MicrosoftScheme, "Microsoft", options =>
        {
            options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            options.ClientId = clientId ?? string.Empty;
            options.ClientSecret = clientSecret ?? string.Empty;
            options.CallbackPath = callbackPath;

            // Use authorization code flow (most secure for server-side apps)
            options.ResponseType = OpenIdConnectResponseType.Code;

            // Request profile, email, and openid scopes
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");

            // Save tokens for potential future use (e.g., calling MS Graph)
            options.SaveTokens = true;

            // Use the application cookie scheme to persist the identity
            options.SignInScheme = "Cookies";

            // Map Microsoft claims to standard claims
            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");

            // Validate issuer based on tenant configuration
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = tenantId != "common",
                NameClaimType = "name",
                RoleClaimType = "roles"
            };

            options.Events = new OpenIdConnectEvents
            {
                OnTokenValidated = context =>
                {
                    // Add a custom claim to identify the SSO provider
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        identity.AddClaim(new Claim("auth_provider", "Microsoft"));
                    }
                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("MicrosoftAuth");

                    logger.LogWarning(
                        context.Failure,
                        "Microsoft authentication failed for remote IP {RemoteIp}",
                        context.HttpContext.Connection.RemoteIpAddress);

                    context.Response.Redirect("/account/login?error=microsoft-auth-failed");
                    context.HandleResponse();
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    // Redirect to Microsoft's logout endpoint
                    var logoutUri = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/logout";
                    var postLogoutUri = context.Properties.RedirectUri;

                    if (!string.IsNullOrEmpty(postLogoutUri))
                    {
                        logoutUri += $"?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutUri)}";
                    }

                    context.Response.Redirect(logoutUri);
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });

        return builder;
    }

    /// <summary>
    /// Validates that Microsoft OAuth credentials are configured.
    /// Returns true if both ClientId and ClientSecret are non-empty.
    /// </summary>
    public static bool IsMicrosoftAuthConfigured(IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Microsoft:ClientId"];
        var clientSecret = configuration["Authentication:Microsoft:ClientSecret"];

        return !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret)
            && clientId != "your-microsoft-client-id";
    }
}
