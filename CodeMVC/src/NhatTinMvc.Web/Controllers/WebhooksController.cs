using System.Text;
using Microsoft.AspNetCore.Mvc;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Web.Controllers;

/// <summary>
/// Endpoint nhận webhook trạng thái của NhatTin (sandbox dispatch về đây). Khớp path với CodeWebHooks:
/// /webhooks/nhattin/status. Nhận mọi method docs liệt kê (GET/POST/PUT), luôn lưu raw trước rồi ACK.
/// </summary>
[ApiController]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookIngestService _ingest;
    public WebhooksController(IWebhookIngestService ingest) => _ingest = ingest;

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

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var outcome = await _ingest.IngestAsync(Request.Method, body, headers, ct);

        var message = outcome.Duplicate ? "ACK (duplicate ignored)" : "ACK";
        return Ok(new { success = true, message, data = new { received_at = outcome.ReceivedAt } });
    }
}
