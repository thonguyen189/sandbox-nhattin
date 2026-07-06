using System.Globalization;
using System.Text.Json.Serialization;
using NhatTinLogistics.Sdk.Types.Enums;

namespace NhatTinLogistics.Sdk.Webhooks;

/// <summary>Typed incoming webhook payload (bill/webhook.md). NhatTin does NOT sign webhooks — no signature to verify.</summary>
public sealed class WebhookPayload
{
    [JsonPropertyName("bill_no")] public string BillNo { get; set; } = "";
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("status_id")] public int StatusId { get; set; }
    [JsonPropertyName("status_name")] public string? StatusName { get; set; }
    [JsonPropertyName("status_time")] public long StatusTime { get; set; }
    [JsonPropertyName("push_time")] public long PushTime { get; set; }
    [JsonPropertyName("shipping_fee")] public decimal ShippingFee { get; set; }
    [JsonPropertyName("is_partial")] public int IsPartial { get; set; }
    [JsonPropertyName("reason")] public string? Reason { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("dimension_weight")] public double DimensionWeight { get; set; }
    [JsonPropertyName("length")] public double Length { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("expected_at")] public string? ExpectedAt { get; set; }

    [JsonIgnore] public BillStatus Status => StatusId.ToBillStatus();
    [JsonIgnore] public bool IsPartialReturn => IsPartial == 1;
    [JsonIgnore] public DateTimeOffset StatusTimeUtc => DateTimeOffset.FromUnixTimeSeconds(StatusTime);
    [JsonIgnore] public DateTimeOffset PushTimeUtc => DateTimeOffset.FromUnixTimeSeconds(PushTime);

    [JsonIgnore]
    public DateTime? ExpectedAtUtc =>
        DateTime.TryParseExact(ExpectedAt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d : null;
}
