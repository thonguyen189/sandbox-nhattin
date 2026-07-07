using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.Tests;

// Test double for IWebhookDispatcher that records each dispatch call. When a db context is
// supplied it also captures the bill's persisted StatusId at dispatch time, so tests can assert
// that the status change was already committed before the webhook fired.
internal sealed class RecordingWebhookDispatcher : IWebhookDispatcher
{
    private readonly SandboxDbContext? _db;
    public RecordingWebhookDispatcher(SandboxDbContext? db = null) => _db = db;

    public List<(int BillId, int StatusId)> Calls { get; } = new();

    public Task DispatchAsync(int billId, CancellationToken ct)
    {
        var statusId = _db?.Bills
            .Where(b => b.Id == billId)
            .Select(b => b.StatusId)
            .FirstOrDefault() ?? 0;
        Calls.Add((billId, statusId));
        return Task.CompletedTask;
    }
}
