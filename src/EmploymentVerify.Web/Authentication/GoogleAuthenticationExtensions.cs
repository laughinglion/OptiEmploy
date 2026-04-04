using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Extension methods to configure Google OAuth SSO for the application.
/// Google OAuth is used as an optional SSO provider alongside email/password auth.
/// </summary>
public static class GoogleAuthenticationExtensions
{
    /// <summary>
    /// Registers cookie + Google OAuth authentication services.
    /// Client credentials are read from Configuration:Authentication:Google.
    /// </summary>
    public static IServiceCollection AddExternalSsoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var googleSection = configuration.GetSection("Authentication:Google");
        var clientId = googleSection["ClientId"];
        var clientSecret = googleSection["ClientSecret"];
        var callbackPath = googleSection["CallbackPath"] ?? "/signin-google";

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.LogoutPath = "/account/logout";
            options.AccessDeniedPath = "/account/access-denied";
            options.Cookie.Name = "EmploymentVerify.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        })
        .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
        {
            options.ClientId = clientId ?? string.Empty;
            options.ClientSecret = clientSecret ?? string.Empty;
            options.CallbackPath = callbackPath;

            // Request profile and email scopes
            options.Scope.Add("profile");
            options.Scope.Add("email");

            // Save tokens for potential future use (e.g., revocation)
            options.SaveTokens = true;

            // Map Google claims to standard claims
            options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
            options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
            options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
            options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
            options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
            options.ClaimActions.MapJsonKey("picture", "picture");

            options.Events.OnCreatingTicket = context =>
            {
                // Add a custom claim to identify the SSO provider
                if (context.Identity is not null)
                {
                    context.Identity.AddClaim(new Claim("auth_provider", "Google"));
                }
                return Task.CompletedTask;
            };

            options.Events.OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GoogleAuth");

                logger.LogWarning(
                    context.Failure,
                    "Google authentication failed for remote IP {RemoteIp}",
                    context.HttpContext.Connection.RemoteIpAddress);

                context.Response.Redirect("/account/login?error=google-auth-failed");
                context.HandleResponse();
                return Task.CompletedTask;
            };
        });

        // Add Microsoft Entra ID SSO (OpenID Connect)
        authBuilder.AddMicrosoftSsoAuthentication(configuration);

        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        return services;
    }

    /// <summary>
    /// Validates that Google OAuth credentials are configured.
    /// Returns true if both ClientId and ClientSecret are non-empty.
    /// </summary>
    public static bool IsGoogleAuthConfigured(IConfiguration configuration)
    {
        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];

        return !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret)
            && clientId != "your-google-client-id.apps.googleusercontent.com";
    }
}
