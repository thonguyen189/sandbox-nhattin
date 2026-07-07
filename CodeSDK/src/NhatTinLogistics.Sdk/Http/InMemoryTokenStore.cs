using System;

namespace NhatTinLogistics.Sdk.Http;

public sealed class InMemoryTokenStore : ITokenStore
{
    private readonly object _lock = new();
    private string? _access;
    private string? _refresh;
    private DateTimeOffset? _accessExpiresAt;
    private DateTimeOffset? _refreshExpiresAt;

    public string? AccessToken { get { lock (_lock) { return _access; } } }
    public string? RefreshToken { get { lock (_lock) { return _refresh; } } }
    public DateTimeOffset? AccessTokenExpiresAt { get { lock (_lock) { return _accessExpiresAt; } } }
    public DateTimeOffset? RefreshTokenExpiresAt { get { lock (_lock) { return _refreshExpiresAt; } } }

    public void SetTokens(string accessToken, string refreshToken,
                          DateTimeOffset? accessExpiresAt = null,
                          DateTimeOffset? refreshExpiresAt = null)
    {
        lock (_lock)
        {
            _access = accessToken;
            _refresh = refreshToken;
            _accessExpiresAt = accessExpiresAt;
            _refreshExpiresAt = refreshExpiresAt;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _access = null;
            _refresh = null;
            _accessExpiresAt = null;
            _refreshExpiresAt = null;
        }
    }
}
