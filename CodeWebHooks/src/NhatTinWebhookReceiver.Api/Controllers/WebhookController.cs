using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Domain;
using NhatTinWebhookReceiver.Api.Persistence;

namespace NhatTinWebhookReceiver.Api.Controllers;

[ApiController]
public sealed class WebhookController : ControllerBase
{
    private readonly WebhookDbContext _db;
    public WebhookController(WebhookDbContext db) => _db = db;

    // Docs list GET/POST/PUT. Accept all; always store raw evidence first.
    [HttpPost("/webhooks/nhattin/status")]
    [HttpPut("/webhooks/nhattin/status")]
    [HttpGet("/webhooks/nhattin/status")]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        string body = "";
        if (Request.ContentLength is > 0)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            body = await reader.ReadToEndAsync();
        }

        var record = new ReceivedWebhook
        {
            ReceivedAt = DateTimeOffset.UtcNow,
            HttpMethod = Request.Method,
            HeadersJson = JsonSerializer.Serialize(Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            RawBody = body
        };

        TryParse(body, record);

        // Self-dedupe: NhatTin sends no idempotency key, so we dedupe on the derived key.
        // If an event with the same key already landed, ACK idempotently without a 2nd row.
        if (record.DedupeKey is not null &&
            await _db.ReceivedWebhooks.AnyAsync(w => w.DedupeKey == record.DedupeKey, ct))
        {
            return DuplicateAck(record);
        }

        _db.ReceivedWebhooks.Add(record);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (record.DedupeKey is not null)
        {
            // Race backstop: a concurrent request inserted the same DedupeKey first and the
            // filtered unique index rejected ours. Treat as a duplicate — never a 500.
            return DuplicateAck(record);
        }

        return Ok(new { success = true, message = "ACK", data = new { received_at = record.ReceivedAt } });
    }

    private IActionResult DuplicateAck(ReceivedWebhook record) =>
        Ok(new { success = true, message = "ACK (duplicate ignored)", data = new { received_at = record.ReceivedAt } });

    private static void TryParse(string body, ReceivedWebhook record)
    {
        if (string.IsNullOrWhiteSpace(body)) return;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("bill_no", out var billNo)) record.BillNo = billNo.GetString();
            if (root.TryGetProperty("status_id", out var sid) && sid.TryGetInt32(out var sidVal)) record.StatusId = sidVal;
            if (root.TryGetProperty("status_name", out var sname)) record.StatusName = sname.GetString();
            if (root.TryGetProperty("ref_code", out var rc)) record.RefCode = rc.GetString();
            // Unix epoch seconds; tolerate missing/wrong-typed by leaving null.
            if (root.TryGetProperty("status_time", out var st) && st.TryGetInt64(out var stVal)) record.StatusTime = stVal;
            if (root.TryGetProperty("push_time", out var pt) && pt.TryGetInt64(out var ptVal)) record.PushTime = ptVal;
            record.IsValidPayload = record.BillNo is not null && record.StatusId is not null;

            // Derive the self-dedupe key only when all three parts are present; otherwise the
            // event stays un-dedupable and is always stored.
            if (record.BillNo is not null && record.StatusId is not null && record.StatusTime is not null)
                record.DedupeKey = $"{record.BillNo}|{record.StatusId}|{record.StatusTime}";
        }
        catch (Exception)
        {
            // Any parse-time failure (malformed JSON, non-object root like an array/scalar,
            // or a field present with an unexpected type) must never bubble out of here —
            // store raw anyway; never "fix" it.
            record.IsValidPayload = false;
        }
    }
}
