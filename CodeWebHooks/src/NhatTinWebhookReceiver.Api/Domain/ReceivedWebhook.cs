namespace NhatTinWebhookReceiver.Api.Domain;

public class ReceivedWebhook
{
    public int Id { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string HttpMethod { get; set; } = string.Empty;
    public string HeadersJson { get; set; } = string.Empty;
    public string RawBody { get; set; } = string.Empty;
    public bool IsValidPayload { get; set; }
    public string? BillNo { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? RefCode { get; set; }
}
