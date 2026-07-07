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

    // Unix epoch seconds carried by the webhook. NhatTin sends no idempotency key,
    // so these are the natural dedupe/ordering keys — persist them, don't drop into raw only.
    public long? StatusTime { get; set; }
    public long? PushTime { get; set; }

    // Self-dedupe key: "{BillNo}|{StatusId}|{StatusTime}" when all three are present; else null
    // (un-dedupable events are always stored).
    public string? DedupeKey { get; set; }
}
