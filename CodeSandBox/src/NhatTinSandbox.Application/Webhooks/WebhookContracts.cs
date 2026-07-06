namespace NhatTinSandbox.Application.Webhooks;

public interface IWebhookDispatcher
{
    // Sends the current status of the bill to all active subscriptions and logs each attempt.
    Task DispatchAsync(int billId, CancellationToken ct);
}
