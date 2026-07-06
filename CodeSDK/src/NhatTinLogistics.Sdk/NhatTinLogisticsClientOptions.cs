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

    /// <summary>When true, the SDK signs in lazily and refreshes the token on 401.</summary>
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
