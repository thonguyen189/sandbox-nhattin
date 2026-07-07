using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>Element of /v3/bill/destroy data. Note the documented camelCase "doCode".</summary>
public sealed class CancelResult
{
    [JsonPropertyName("doCode")] public string DoCode { get; set; } = "";
    [JsonPropertyName("message")] public string? Message { get; set; }
}

/// <summary>
/// data of /v3/bill/destroy. Verified live 2026-07-07: the API returns an OBJECT
/// <c>{ "success": [{doCode,message}], "failed": [...] }</c> (per-bill outcomes), NOT a bare array.
/// </summary>
public sealed class CancelResponse
{
    [JsonPropertyName("success")] public List<CancelResult> Succeeded { get; set; } = new();
    [JsonPropertyName("failed")] public List<CancelResult> Failed { get; set; } = new();
}
