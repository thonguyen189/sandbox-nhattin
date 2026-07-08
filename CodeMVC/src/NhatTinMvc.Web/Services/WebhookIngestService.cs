using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NhatTinLogistics.Sdk.Webhooks;
using NhatTinMvc.Web.Data;
using NhatTinMvc.Web.Data.Entities;
using NhatTinMvc.Web.Hubs;

namespace NhatTinMvc.Web.Services;

/// <summary>
/// Nhận webhook: lưu raw → parse bằng NhatTinWebhookParser (SDK) → dedupe theo {bill_no|status_id|status_time}
/// → lưu event, cập nhật TrackedBill.Last*, đẩy SignalR. Không ném ra ngoài — luôn ACK được.
/// </summary>
public sealed class WebhookIngestService : IWebhookIngestService
{
    private readonly MvcDbContext _db;
    private readonly IHubContext<BillStatusHub, IBillStatusClient> _hub;
    private readonly ILogger<WebhookIngestService> _logger;

    public WebhookIngestService(
        MvcDbContext db,
        IHubContext<BillStatusHub, IBillStatusClient> hub,
        ILogger<WebhookIngestService> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task<WebhookIngestOutcome> IngestAsync(
        string httpMethod, string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new BillStatusEvent
        {
            Source = "Webhook",
            ReceivedAt = now,
            RawPayload = rawBody
        };

        var parsed = NhatTinWebhookParser.TryParse(rawBody, out var payload);
        if (parsed)
        {
            record.BillCode = payload.BillNo;
            record.StatusId = payload.StatusId;
            record.StatusName = string.IsNullOrEmpty(payload.StatusName) ? payload.Status.ToString() : payload.StatusName;
            record.StatusTime = payload.StatusTime;
            record.PushTime = payload.PushTime;
            record.Reason = payload.Reason;
            if (!string.IsNullOrEmpty(payload.BillNo) && payload.StatusTime > 0)
                record.DedupeKey = $"{payload.BillNo}|{payload.StatusId}|{payload.StatusTime}";
        }

        // Self-dedupe (NhatTin không có idempotency key).
        if (record.DedupeKey is not null &&
            await _db.BillStatusEvents.AnyAsync(e => e.DedupeKey == record.DedupeKey, ct))
        {
            return new WebhookIngestOutcome(true, parsed, record.BillCode, record.StatusId, record.StatusName, now);
        }

        // Khớp bill đã theo dõi và cập nhật trạng thái mới nhất.
        TrackedBill? bill = record.BillCode is null ? null
            : await _db.TrackedBills.FirstOrDefaultAsync(b => b.BillCode == record.BillCode, ct);
        if (bill is not null)
        {
            record.TrackedBillId = bill.Id;
            bill.LastStatusId = record.StatusId;
            bill.LastStatusName = record.StatusName;
            bill.LastStatusAt = record.StatusTime is > 0
                ? DateTimeOffset.FromUnixTimeSeconds(record.StatusTime.Value)
                : now;
        }

        _db.BillStatusEvents.Add(record);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (record.DedupeKey is not null)
        {
            // Race: một request khác chèn cùng DedupeKey trước; unique index chặn. Coi như trùng — không 500.
            return new WebhookIngestOutcome(true, parsed, record.BillCode, record.StatusId, record.StatusName, now);
        }

        // Đẩy realtime cho các browser đang mở.
        if (!string.IsNullOrEmpty(record.BillCode))
        {
            await _hub.Clients.All.BillStatusChanged(new BillStatusUpdate(
                record.BillCode, record.StatusId, record.StatusName, record.StatusTime, now));
        }

        _logger.LogInformation("Webhook {Method} bill={Bill} status={Status} parsed={Parsed}",
            httpMethod, record.BillCode, record.StatusId, parsed);
        return new WebhookIngestOutcome(false, parsed, record.BillCode, record.StatusId, record.StatusName, now);
    }
}
