namespace NhatTinSandbox.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
