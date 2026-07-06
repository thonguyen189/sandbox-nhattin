using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>Element of /v3/bill/destroy data. Note the documented camelCase "doCode".</summary>
public sealed class CancelResult
{
    [JsonPropertyName("doCode")] public string DoCode { get; set; } = "";
    [JsonPropertyName("message")] public string? Message { get; set; }
}
