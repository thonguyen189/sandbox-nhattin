namespace NhatTinLogistics.Sdk.Http;

public sealed class InMemoryTokenStore : ITokenStore
{
    private readonly object _lock = new();
    private string? _access;
    private string? _refresh;

    public string? AccessToken { get { lock (_lock) { return _access; } } }
    public string? RefreshToken { get { lock (_lock) { return _refresh; } } }

    public void SetTokens(string accessToken, string refreshToken)
    {
        lock (_lock) { _access = accessToken; _refresh = refreshToken; }
    }

    public void Clear()
    {
        lock (_lock) { _access = null; _refresh = null; }
    }
}
