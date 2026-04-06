using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Handles email/password login form submissions and issues the application session cookie.
/// </summary>
[Route("account")]
[ApiController]
public class LoginController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LoginController> _logger;
    private readonly IServerTokenStore _tokenStore;

    public LoginController(IHttpClientFactory httpClientFactory, ILogger<LoginController> logger, IServerTokenStore tokenStore)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// Handles the login form POST from Login.razor.
    /// Calls the API to validate credentials, then signs the user in with a cookie.
    /// POST /account/login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password,
        [FromQuery] string? returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Redirect("/account/login?error=invalid-credentials");

        try
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login failed for {Email}: API returned {StatusCode}", email, response.StatusCode);
                return Redirect("/account/login?error=invalid-credentials");
            }

            var loginResult = await response.Content.ReadFromJsonAsync<ApiLoginResponse>();
            if (loginResult is null || string.IsNullOrEmpty(loginResult.Token))
                return Redirect("/account/login?error=invalid-credentials");

            await SignInWithCookieAsync(loginResult);

            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
                returnUrl = "/";

            _logger.LogInformation("User {Email} signed in successfully", email);
            return LocalRedirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", email);
            return Redirect("/account/login?error=server-error");
        }
    }

    internal async Task SignInWithCookieAsync(ApiLoginResponse loginResult)
    {
        var sessionId = Guid.NewGuid().ToString();
        _tokenStore.Store(sessionId, loginResult.Token, TimeSpan.FromHours(8));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
            new(ClaimTypes.Email, loginResult.Email),
            new(ClaimTypes.Name, loginResult.FullName),
            new(ClaimTypes.Role, loginResult.Role),
            new("session_id", sessionId)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }
}

/// <summary>Matches the API's LoginResponse shape.</summary>
public sealed record ApiLoginResponse(string Token, Guid UserId, string Email, string FullName, string Role);
