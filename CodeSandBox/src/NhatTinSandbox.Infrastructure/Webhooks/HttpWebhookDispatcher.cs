using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Infrastructure.Webhooks;

public sealed class HttpWebhookDispatcher : IWebhookDispatcher
{
    private readonly SandboxDbContext _db;
    private readonly IHttpClientFactory _httpFactory;

    public HttpWebhookDispatcher(SandboxDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    public async Task DispatchAsync(int billId, CancellationToken ct)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId, ct);
        if (bill is null) return;

        var latest = await _db.BillStatusHistories
            .Where(h => h.BillId == billId)
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var payload = new
        {
            weight = bill.Weight,
            bill_no = bill.BillCode,
            status_time = ((DateTimeOffset)(latest?.ChangedAt ?? now)).ToUnixTimeSeconds(),
            shipping_fee = (double)bill.TotalFee,
            is_partial = 0,
            status_name = latest?.StatusName ?? $"Status {bill.StatusId}",
            status_id = bill.StatusId,
            dimension_weight = 0d,
            length = bill.Length,
            width = bill.Width,
            height = bill.Height,
            push_time = now.ToUnixTimeSeconds(),
            ref_code = bill.RefCode,
            expected_at = (bill.ExpectedAt ?? now).ToString("yyyy-MM-dd HH:mm:ss"),
            reason = latest?.Reason
        };
        var json = JsonSerializer.Serialize(payload);

        var subs = await _db.WebhookSubscriptions
            .Where(s => s.IsActive && s.PartnerId == 123736)
            .ToListAsync(ct);

        var client = _httpFactory.CreateClient("webhook");
        foreach (var sub in subs)
        {
            var log = new WebhookDeliveryLog
            {
                BillId = bill.Id,
                BillCode = bill.BillCode,
                SubscriptionId = sub.Id,
                CallbackUrl = sub.CallbackUrl,
                PayloadJson = json,
                AttemptedAt = now
            };
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await client.PostAsync(sub.CallbackUrl, content, ct);
                log.HttpStatusCode = (int)resp.StatusCode;
                log.Success = resp.IsSuccessStatusCode;
                log.ResponseBody = await resp.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ResponseBody = ex.Message;
            }
            _db.WebhookDeliveryLogs.Add(log);
        }
        await _db.SaveChangesAsync(ct);
    }
}
