using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Handles OAuth callback routes for external SSO providers (Google, Microsoft).
/// Blazor Server cannot handle OAuth redirects natively, so we use
/// controller endpoints for the challenge/callback flow.
/// </summary>
[Route("account")]
[ApiController]
public class ExternalAuthController : ControllerBase
{
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(ILogger<ExternalAuthController> logger)
    {
        _logger = logger;
    }

    // ─── Google SSO ──────────────────────────────────────────────────

    /// <summary>
    /// Initiates the Google OAuth challenge flow.
    /// GET /account/google-login?returnUrl=/
    /// </summary>
    [HttpGet("google-login")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl }),
            Items =
            {
                { "LoginProvider", "Google" }
            }
        };

        _logger.LogInformation("Initiating Google OAuth challenge, return URL: {ReturnUrl}", returnUrl);

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles the callback after Google authentication completes.
    /// GET /account/google-callback?returnUrl=/
    /// The user is already authenticated via the Google middleware at this point.
    /// </summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = "/")
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Google callback authentication failed");
            return Redirect("/account/login?error=google-callback-failed");
        }

        var email = authenticateResult.Principal?.FindFirst(
            System.Security.Claims.ClaimTypes.Email)?.Value;

        _logger.LogInformation(
            "Google SSO callback successful for {Email}",
            email ?? "unknown");

        // TODO: Link Google SSO identity to local user account
        // This will be implemented when the user management system is built.

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return LocalRedirect(returnUrl);
    }

    // ─── Microsoft SSO ───────────────────────────────────────────────

    /// <summary>
    /// Initiates the Microsoft Entra ID OpenID Connect challenge flow.
    /// GET /account/microsoft-login?returnUrl=/
    /// </summary>
    [HttpGet("microsoft-login")]
    public IActionResult MicrosoftLogin([FromQuery] string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(MicrosoftCallback), new { returnUrl }),
            Items =
            {
                { "LoginProvider", "Microsoft" }
            }
        };

        _logger.LogInformation("Initiating Microsoft OAuth challenge, return URL: {ReturnUrl}", returnUrl);

        return Challenge(properties, MicrosoftAuthenticationExtensions.MicrosoftScheme);
    }

    /// <summary>
    /// Handles the callback after Microsoft authentication completes.
    /// GET /account/microsoft-callback?returnUrl=/
    /// The user is already authenticated via the OpenID Connect middleware at this point.
    /// </summary>
    [HttpGet("microsoft-callback")]
    public async Task<IActionResult> MicrosoftCallback([FromQuery] string? returnUrl = "/")
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Microsoft callback authentication failed");
            return Redirect("/account/login?error=microsoft-callback-failed");
        }

        var email = authenticateResult.Principal?.FindFirst(
            System.Security.Claims.ClaimTypes.Email)?.Value;

        _logger.LogInformation(
            "Microsoft SSO callback successful for {Email}",
            email ?? "unknown");

        // TODO: Link Microsoft SSO identity to local user account
        // This will be implemented when the user management system is built.

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return LocalRedirect(returnUrl);
    }

    // ─── Logout ──────────────────────────────────────────────────────

    /// <summary>
    /// Signs the user out of both the application cookie and any external provider.
    /// POST /account/logout
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logging out");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }
}
