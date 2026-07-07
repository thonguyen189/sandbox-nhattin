using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

public sealed class FeeOption
{
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("total_fee")] public decimal TotalFee { get; set; }
    [JsonPropertyName("main_fee")] public decimal MainFee { get; set; }
    [JsonPropertyName("insur_fee")] public decimal InsurFee { get; set; }
    [JsonPropertyName("remote_fee")] public decimal RemoteFee { get; set; }
    [JsonPropertyName("cod_fee")] public decimal CodFee { get; set; }
    // Sandbox returns service_id:null on calc-fee (verified live 2026-07-07), so this is nullable.
    [JsonPropertyName("service_id")] public int? ServiceId { get; set; }
    [JsonPropertyName("service_name")] public string? ServiceName { get; set; }
    [JsonPropertyName("lead_time")] public string? LeadTime { get; set; }
}
