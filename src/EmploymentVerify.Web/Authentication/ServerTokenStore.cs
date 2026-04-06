using Microsoft.Extensions.Caching.Memory;

namespace EmploymentVerify.Web.Authentication;

/// <summary>
/// Stores JWT tokens server-side keyed by a session ID.
/// Only the session ID is stored in the cookie — the JWT never leaves the server.
/// </summary>
public interface IServerTokenStore
{
    void Store(string sessionId, string jwt, TimeSpan expiry);
    string? Get(string sessionId);
    void Remove(string sessionId);
}

public class MemoryServerTokenStore : IServerTokenStore
{
    private readonly IMemoryCache _cache;
    public MemoryServerTokenStore(IMemoryCache cache) => _cache = cache;

    public void Store(string sessionId, string jwt, TimeSpan expiry) =>
        _cache.Set($"jwt:{sessionId}", jwt, expiry);

    public string? Get(string sessionId) =>
        _cache.TryGetValue($"jwt:{sessionId}", out string? jwt) ? jwt : null;

    public void Remove(string sessionId) =>
        _cache.Remove($"jwt:{sessionId}");
}
