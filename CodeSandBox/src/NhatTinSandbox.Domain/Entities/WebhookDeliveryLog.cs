namespace NhatTinSandbox.Domain.Entities;

public class WebhookDeliveryLog
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public string BillCode { get; set; } = string.Empty;
    public int SubscriptionId { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public bool Success { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
}
