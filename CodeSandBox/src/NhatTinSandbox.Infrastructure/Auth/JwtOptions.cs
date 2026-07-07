namespace NhatTinSandbox.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "nhattin-sandbox";
    public string Audience { get; set; } = "nhattin-sandbox";
    public string SigningKey { get; set; } = "";
    public int AccessTtlSeconds { get; set; } = 86400;   // 24h — matches real Nhất Tín token_expires_in
    public int RefreshTtlSeconds { get; set; } = 604800; // 7d  — matches real Nhất Tín refresh_expires_in
}
