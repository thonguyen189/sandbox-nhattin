namespace NhatTinSandbox.Domain.Entities;

public class WebhookSubscription
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
