using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Requests;

/// <summary>POST /v3/bill/update-shipping — per updatebill.md. PartnerId defaults from options when null.</summary>
public sealed class UpdateBillRequest
{
    [JsonPropertyName("partner_id")] public int? PartnerId { get; set; }
    [JsonPropertyName("bill_code")] public string BillCode { get; set; } = "";
    [JsonPropertyName("cod_amount")] public decimal? CodAmount { get; set; }
    [JsonPropertyName("cargo_value")] public double? CargoValue { get; set; }
    [JsonPropertyName("weight")] public double? Weight { get; set; }
    [JsonPropertyName("length")] public double? Length { get; set; }
    [JsonPropertyName("height")] public double? Height { get; set; }
    [JsonPropertyName("width")] public double? Width { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("receiver_phone")] public string? ReceiverPhone { get; set; }
    [JsonPropertyName("receiver_name")] public string? ReceiverName { get; set; }
    [JsonPropertyName("receiver_address")] public string? ReceiverAddress { get; set; }
    [JsonPropertyName("package_no")] public int? PackageNo { get; set; }
    [JsonPropertyName("is_return_doc")] public int? IsReturnDoc { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("is_installation")] public int? IsInstallation { get; set; }
}
