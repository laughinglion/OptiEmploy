using System.Security.Claims;
using System.Net.Http.Headers;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// DelegatingHandler that attaches the JWT stored in the user's cookie claims
/// to every outgoing API request as a Bearer token.
/// This bridges Blazor Server's cookie authentication with the API's JWT auth.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User
            .FindFirst("access_token")?.Value;

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
