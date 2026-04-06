namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// DelegatingHandler that retrieves the JWT from the server-side token store
/// (keyed by the session ID in the user's cookie) and attaches it as a Bearer
/// token to every outgoing API request.
/// This bridges Blazor Server's cookie authentication with the API's JWT auth.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServerTokenStore _tokenStore;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, IServerTokenStore tokenStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenStore = tokenStore;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionId = _httpContextAccessor.HttpContext?.User.FindFirst("session_id")?.Value;
        if (!string.IsNullOrEmpty(sessionId))
        {
            var token = _tokenStore.Get(sessionId);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        return base.SendAsync(request, cancellationToken);
    }
}
