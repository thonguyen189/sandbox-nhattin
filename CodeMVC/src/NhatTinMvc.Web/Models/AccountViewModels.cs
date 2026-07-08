using System.ComponentModel.DataAnnotations;

namespace NhatTinMvc.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Nhập tài khoản")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    /// <summary>Thông báo lỗi/thành công hiển thị trên form.</summary>
    public string? Message { get; set; }
    public bool IsError { get; set; }
}

/// <summary>Trạng thái đăng nhập hiển thị (token đã mask + partner_id).</summary>
public class AuthStatusViewModel
{
    public bool IsAuthenticated { get; set; }
    public int? PartnerId { get; set; }
    public string? Username { get; set; }
    public string? AccessTokenMasked { get; set; }
    public string? RefreshTokenMasked { get; set; }
    public DateTimeOffset? AccessTokenExpiresAt { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public DateTimeOffset? LoginAtUtc { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
}
