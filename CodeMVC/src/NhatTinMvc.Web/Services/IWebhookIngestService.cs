namespace NhatTinMvc.Web.Services;

/// <summary>
/// Nhận webhook trạng thái: lưu raw → parse bằng NhatTinWebhookParser của SDK → dedupe → lưu event,
/// cập nhật TrackedBill.Last*, đẩy SignalR. Trả outcome để controller ACK đúng (mới / trùng).
/// </summary>
public interface IWebhookIngestService
{
    Task<WebhookIngestOutcome> IngestAsync(
        string httpMethod,
        string rawBody,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default);
}

public sealed record WebhookIngestOutcome(
    bool Duplicate,
    bool ParsedOk,
    string? BillCode,
    int? StatusId,
    string? StatusName,
    DateTimeOffset ReceivedAt);
