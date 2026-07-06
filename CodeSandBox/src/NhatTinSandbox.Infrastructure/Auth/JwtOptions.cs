namespace NhatTinSandbox.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "nhattin-sandbox";
    public string Audience { get; set; } = "nhattin-sandbox";
    public string SigningKey { get; set; } = "";
    public int AccessTtlSeconds { get; set; } = 900;
    public int RefreshTtlSeconds { get; set; } = 3600;
}
