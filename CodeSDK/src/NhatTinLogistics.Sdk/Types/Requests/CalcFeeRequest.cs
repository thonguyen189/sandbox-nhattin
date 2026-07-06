using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Requests;

/// <summary>POST /v3/bill/calc-fee — per calcfee.md. New admin uses *_id fields; legacy uses province/district names.</summary>
public sealed class CalcFeeRequest
{
    [JsonPropertyName("partner_id")] public int? PartnerId { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("width")] public double? Width { get; set; }
    [JsonPropertyName("length")] public double? Length { get; set; }
    [JsonPropertyName("height")] public double? Height { get; set; }
    [JsonPropertyName("service_id")] public int? ServiceId { get; set; }
    [JsonPropertyName("payment_method_id")] public int PaymentMethodId { get; set; }
    [JsonPropertyName("cod_amount")] public double? CodAmount { get; set; }
    [JsonPropertyName("cargo_value")] public double? CargoValue { get; set; }
    // New administrative units (after 2025-07-01)
    [JsonPropertyName("s_province_id")] public string? SProvinceId { get; set; }
    [JsonPropertyName("s_ward_id")] public string? SWardId { get; set; }
    [JsonPropertyName("r_province_id")] public string? RProvinceId { get; set; }
    [JsonPropertyName("r_ward_id")] public string? RWardId { get; set; }
    // Legacy administrative units
    [JsonPropertyName("s_province")] public string? SProvince { get; set; }
    [JsonPropertyName("s_district")] public string? SDistrict { get; set; }
    [JsonPropertyName("r_province")] public string? RProvince { get; set; }
    [JsonPropertyName("r_district")] public string? RDistrict { get; set; }
}
