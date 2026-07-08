namespace NhatTinMvc.Web.Data.Entities;

/// <summary>
/// Một sự kiện trạng thái nhận được — từ webhook (sandbox dispatch) hoặc từ một lần gọi tracking.
/// Lưu raw trước, parse sau; dedupe theo <see cref="DedupeKey"/> vì NhatTin không có idempotency key.
/// </summary>
public class BillStatusEvent
{
    public int Id { get; set; }

    /// <summary>Khớp với <see cref="TrackedBill"/> theo BillCode; null nếu event tới trước khi có bill.</summary>
    public int? TrackedBillId { get; set; }
    public TrackedBill? TrackedBill { get; set; }

    public string? BillCode { get; set; }
    public int StatusId { get; set; }
    public string? StatusName { get; set; }
    public long? StatusTime { get; set; }
    public long? PushTime { get; set; }
    public string? Reason { get; set; }

    /// <summary>"Webhook" | "Tracking".</summary>
    public string Source { get; set; } = "Webhook";

    public DateTimeOffset ReceivedAt { get; set; }
    public string? RawPayload { get; set; }

    /// <summary>{bill_no}|{status_id}|{status_time}; null khi thiếu phần → không dedupe được.</summary>
    public string? DedupeKey { get; set; }
}
