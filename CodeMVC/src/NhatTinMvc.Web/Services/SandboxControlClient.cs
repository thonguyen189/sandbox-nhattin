using System.Text;
using System.Text.Json;

namespace NhatTinMvc.Web.Services;

/// <summary>
/// Gọi endpoint điều khiển của sandbox (KHÔNG thuộc API NhatTin thật):
/// POST /sandbox/bills/{billCode}/simulate-status { status_id, reason }.
/// Sandbox đổi trạng thái bill và bắn webhook tới mọi subscription (gồm MVC).
/// </summary>
public sealed class SandboxControlClient : ISandboxControl
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<SandboxControlClient> _logger;

    public SandboxControlClient(HttpClient http, ILogger<SandboxControlClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<SimulateStatusResult> SimulateStatusAsync(string billCode, int statusId, string? reason, CancellationToken ct = default)
    {
        var url = $"/sandbox/bills/{Uri.EscapeDataString(billCode)}/simulate-status";
        var payload = JsonSerializer.Serialize(new { status_id = statusId, reason }, Json);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            using var resp = await _http.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var (success, message, returnedStatus) = ParseEnvelope(body);
            if (!resp.IsSuccessStatusCode && message is null)
                message = $"Sandbox trả HTTP {(int)resp.StatusCode}.";
            return new SimulateStatusResult(success && resp.IsSuccessStatusCode, message, returnedStatus ?? statusId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không gọi được simulate-status cho {BillCode}", billCode);
            return new SimulateStatusResult(false, $"Không kết nối được sandbox: {ex.Message}", null);
        }
    }

    private static (bool success, string? message, int? statusId) ParseEnvelope(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return (false, null, null);
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var success = root.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.True;
            string? message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
            int? statusId = null;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("status_id", out var sid) && sid.TryGetInt32(out var sidVal))
                statusId = sidVal;
            return (success, message, statusId);
        }
        catch (JsonException)
        {
            return (false, null, null);
        }
    }
}
