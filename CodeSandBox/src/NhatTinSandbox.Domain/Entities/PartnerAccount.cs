namespace NhatTinSandbox.Domain.Entities;

public class PartnerAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int PartnerId { get; set; }
    public bool IsActive { get; set; } = true;
}
