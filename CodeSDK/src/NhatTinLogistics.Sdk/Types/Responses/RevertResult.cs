using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class RevertResult
{
    [JsonPropertyName("success")] public List<string> Success { get; set; } = new();
    [JsonPropertyName("failed")] public List<string> Failed { get; set; } = new();
}
