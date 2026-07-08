namespace NhatTinMvc.Web.Services;

/// <summary>
/// Trạng thái đăng nhập demo (một người vận hành). Singleton — giữ partner_id + thông tin hiển thị
/// qua các request. Token thật nằm trong ITokenStore của SDK; đây chỉ là phần bổ trợ để hiển thị.
/// </summary>
public sealed class DemoAuthState
{
    public int? PartnerId { get; set; }
    public string? Username { get; set; }
    public DateTimeOffset? LoginAtUtc { get; set; }

    public void Clear()
    {
        PartnerId = null;
        Username = null;
        LoginAtUtc = null;
    }
}
