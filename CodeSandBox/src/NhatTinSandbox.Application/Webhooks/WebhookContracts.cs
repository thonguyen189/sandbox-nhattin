namespace NhatTinSandbox.Application.Webhooks;

// Field set from NhatTinAPIDocumentation/vi/bill/webhook.md.
public sealed record WebhookPayload(
    double Weight,
    string BillNo,
    long StatusTime,
    decimal ShippingFee,
    int IsPartial,
    string StatusName,
    int StatusId,
    double DimensionWeight,
    double Length,
    double Width,
    double Height,
    long PushTime,
    string? RefCode,
    string ExpectedAt,
    string? Reason);

public interface IWebhookDispatcher
{
    // Sends the current status of the bill to all active subscriptions and logs each attempt.
    Task DispatchAsync(int billId, CancellationToken ct);
}
