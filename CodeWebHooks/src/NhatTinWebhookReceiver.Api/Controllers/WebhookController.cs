using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
            body = await reader.ReadToEndAsync(ct);
        }

        var record = new ReceivedWebhook
        {
            ReceivedAt = DateTimeOffset.UtcNow,
            HttpMethod = Request.Method,
            HeadersJson = JsonSerializer.Serialize(Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
            RawBody = body
        };

        TryParse(body, record);

        _db.ReceivedWebhooks.Add(record);
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = "ACK", data = new { received_at = record.ReceivedAt } });
    }

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
            record.IsValidPayload = record.BillNo is not null && record.StatusId is not null;
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
