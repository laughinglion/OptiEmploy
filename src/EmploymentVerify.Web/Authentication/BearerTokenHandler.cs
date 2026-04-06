using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// DelegatingHandler that retrieves the JWT from the server-side token store
/// (keyed by the session ID in the user's cookie) and attaches it as a Bearer
/// token to every outgoing API request.
/// On 401 responses, attempts to refresh the JWT using the refresh token cookie.
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

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sessionId = _httpContextAccessor.HttpContext?.User.FindFirst("session_id")?.Value;
        if (!string.IsNullOrEmpty(sessionId))
        {
            var token = _tokenStore.Get(sessionId);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // If we got a 401 and this isn't already a refresh request, try to refresh the token
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !string.IsNullOrEmpty(sessionId)
            && request.RequestUri?.PathAndQuery.Contains("/auth/refresh") != true)
        {
            var newToken = await TryRefreshTokenAsync(sessionId, cancellationToken);
            if (!string.IsNullOrEmpty(newToken))
            {
                // Clone the original request with the new token and retry
                var retry = await CloneRequestAsync(request);
                retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response.Dispose();
                response = await base.SendAsync(retry, cancellationToken);
            }
        }

        return response;
    }

    private async Task<string?> TryRefreshTokenAsync(string sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null) return null;

            // The refresh token is stored in an HttpOnly cookie by the API
            var refreshToken = httpContext.Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken)) return null;

            // Build a fresh HttpRequestMessage (don't use the handler's pipeline to avoid recursion)
            var baseAddress = InnerHandler is not null
                ? new Uri(httpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<string>("ApiBaseUrl") ?? "https://localhost:5001")
                : null;

            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(baseAddress!, "/api/auth/refresh"));
            refreshRequest.Content = JsonContent.Create(new { refreshToken });

            using var client = new HttpClient { BaseAddress = baseAddress };
            var refreshResponse = await client.SendAsync(refreshRequest, cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode) return null;

            var result = await refreshResponse.Content.ReadFromJsonAsync<RefreshResult>(cancellationToken: cancellationToken);
            if (result is null || string.IsNullOrEmpty(result.Token)) return null;

            // Update the server-side token store with the new JWT
            _tokenStore.Store(sessionId, result.Token, TimeSpan.FromHours(8));
            return result.Token;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        if (original.Content is not null)
        {
            var content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            if (original.Content.Headers.ContentType is not null)
                clone.Content.Headers.ContentType = original.Content.Headers.ContentType;
        }
        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }

    private sealed record RefreshResult(string Token);
}
