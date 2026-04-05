using System.Net.Http.Json;
using System.Security.Claims;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExternalAuthController> _logger;

    public ExternalAuthController(IHttpClientFactory httpClientFactory, ILogger<ExternalAuthController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ─── Google SSO ──────────────────────────────────────────────────

    [HttpGet("google-login")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Google" } }
        };

        _logger.LogInformation("Initiating Google OAuth challenge, return URL: {ReturnUrl}", returnUrl);
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = "/")
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Google callback authentication failed");
            return Redirect("/account/login?error=google-callback-failed");
        }

        var email = authenticateResult.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? email ?? "Google User";

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Google callback returned no email address");
            return Redirect("/account/login?error=google-callback-failed");
        }

        _logger.LogInformation("Google SSO callback successful for {Email}", email);

        var signedIn = await LinkSsoAndSignInAsync(email, name, "Google");
        if (!signedIn)
            return Redirect("/account/login?error=google-callback-failed");

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return LocalRedirect(returnUrl);
    }

    // ─── Microsoft SSO ───────────────────────────────────────────────

    [HttpGet("microsoft-login")]
    public IActionResult MicrosoftLogin([FromQuery] string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(MicrosoftCallback), new { returnUrl }),
            Items = { { "LoginProvider", "Microsoft" } }
        };

        _logger.LogInformation("Initiating Microsoft OAuth challenge, return URL: {ReturnUrl}", returnUrl);
        return Challenge(properties, MicrosoftAuthenticationExtensions.MicrosoftScheme);
    }

    [HttpGet("microsoft-callback")]
    public async Task<IActionResult> MicrosoftCallback([FromQuery] string? returnUrl = "/")
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Microsoft callback authentication failed");
            return Redirect("/account/login?error=microsoft-callback-failed");
        }

        var email = authenticateResult.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        var name = authenticateResult.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? email ?? "Microsoft User";

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Microsoft callback returned no email address");
            return Redirect("/account/login?error=microsoft-callback-failed");
        }

        _logger.LogInformation("Microsoft SSO callback successful for {Email}", email);

        var signedIn = await LinkSsoAndSignInAsync(email, name, "Microsoft");
        if (!signedIn)
            return Redirect("/account/login?error=microsoft-callback-failed");

        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            returnUrl = "/";

        return LocalRedirect(returnUrl);
    }

    // ─── Logout ──────────────────────────────────────────────────────

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User logging out");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    // ─── Shared SSO linking logic ─────────────────────────────────────

    /// <summary>
    /// Calls the API SSO endpoint to find/create the user, then issues the application cookie.
    /// Returns false if the SSO login could not be completed.
    /// </summary>
    private async Task<bool> LinkSsoAndSignInAsync(string email, string fullName, string provider)
    {
        try
        {
            // Sign out the temporary OAuth cookie before issuing our application cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("/api/auth/sso", new { email, fullName, provider });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SSO API call failed for {Email}: {StatusCode}", email, response.StatusCode);
                return false;
            }

            var loginResult = await response.Content.ReadFromJsonAsync<ApiLoginResponse>();
            if (loginResult is null || string.IsNullOrEmpty(loginResult.Token))
                return false;

            // Issue the application session cookie with the user's claims + JWT
            var loginController = new LoginController(_httpClientFactory, _logger as ILogger<LoginController>
                ?? HttpContext.RequestServices.GetRequiredService<ILogger<LoginController>>());
            loginController.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = HttpContext
            };
            await loginController.SignInWithCookieAsync(loginResult);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSO identity linking for {Email}", email);
            return false;
        }
    }
}
