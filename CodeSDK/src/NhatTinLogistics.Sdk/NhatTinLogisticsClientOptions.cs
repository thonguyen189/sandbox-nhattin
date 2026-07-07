namespace NhatTinLogistics.Sdk;

public sealed class NhatTinLogisticsClientOptions
{
    /// <summary>JWT login account. Required when AutoAuthenticate is true.</summary>
    public string? Username { get; set; }
    /// <summary>JWT login password. Required when AutoAuthenticate is true.</summary>
    public string? Password { get; set; }

    public NhatTinEnvironment Environment { get; set; } = NhatTinEnvironment.Sandbox;

    /// <summary>Overrides Environment host when set (e.g. for tests/self-host).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Default partner_id for calc-fee / update-shipping / print. Supplied by the consumer.</summary>
    public int? PartnerId { get; set; }

    public int TimeoutMilliseconds { get; set; } = 90_000;

    /// <summary>
    /// When true (default), the SDK refreshes the access token proactively just before it expires (using the
    /// TTL from the sign-in/refresh response) instead of waiting for a 401. Disable to keep the reactive-only
    /// behavior. If the TTL is unknown/unparseable, proactive refresh is skipped and the 401 path still applies.
    /// </summary>
    public bool EnableProactiveRefresh { get; set; } = true;

    /// <summary>How long before the access token's expiry to refresh it proactively. Default 60 seconds.</summary>
    public TimeSpan TokenExpirySkew { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When true (default), the SDK retries transient failures (transport errors, timeouts, HTTP 5xx/429/408)
    /// for idempotent calls only (GET plus calc-fee/sign-in/refresh-token). Write calls — create, update-shipping,
    /// destroy, revert-bill — are never retried, to avoid duplicating a side effect.
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>Maximum number of retries after the initial attempt (default 3 → up to 4 total attempts).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay for exponential backoff (default 200ms → ~200/400/800ms across retries, plus jitter).</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Upper bound on a single backoff delay (default 5s).</summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When true (default), the SDK signs in lazily using Username/Password and refreshes the token on 401.
    /// When false, the caller fully manages auth: seed a token via <c>client.Tokens.SetTokens(accessToken, refreshToken)</c>.
    /// The SDK then attaches the seeded token but never signs in or refreshes; a 401 is returned as
    /// <c>IsSuccess=false</c>, and the caller refreshes via <c>client.Auth.RefreshTokenAsync(...)</c> and re-seeds
    /// <c>client.Tokens</c>. Username/Password are not required in this mode.
    /// </summary>
    public bool AutoAuthenticate { get; set; } = true;

    public string ResolveBaseUrl()
        => !string.IsNullOrWhiteSpace(BaseUrl)
            ? BaseUrl!.TrimEnd('/')
            : Environment == NhatTinEnvironment.Production
                ? "https://apiws.ntlogistics.vn"
                : "https://apisandbox.ntlogistics.vn";

    public void Validate()
    {
        if (AutoAuthenticate && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)))
            throw new ArgumentException("Username and Password are required when AutoAuthenticate is enabled.");
    }
}
