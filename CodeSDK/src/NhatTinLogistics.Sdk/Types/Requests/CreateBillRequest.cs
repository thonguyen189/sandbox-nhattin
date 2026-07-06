using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Requests;

/// <summary>POST /v3/bill/create — fields per NhatTinAPIDocumentation/vi/bill/createbill.md.</summary>
public sealed class CreateBillRequest
{
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("package_no")] public int? PackageNo { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("length")] public double Length { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("service_id")] public int ServiceId { get; set; }
    [JsonPropertyName("payment_method_id")] public int PaymentMethodId { get; set; }
    [JsonPropertyName("is_return_doc")] public int? IsReturnDoc { get; set; }
    [JsonPropertyName("cod_amount")] public decimal? CodAmount { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("cargo_value")] public double? CargoValue { get; set; }
    [JsonPropertyName("cargo_type_id")] public int CargoTypeId { get; set; }
    [JsonPropertyName("s_name")] public string SName { get; set; } = "";
    [JsonPropertyName("s_phone")] public string SPhone { get; set; } = "";
    [JsonPropertyName("s_address")] public string SAddress { get; set; } = "";
    [JsonPropertyName("s_province_code")] public string SProvinceCode { get; set; } = "";
    [JsonPropertyName("s_ward_code")] public string SWardCode { get; set; } = "";
    [JsonPropertyName("is_return_org")] public int? IsReturnOrg { get; set; }
    [JsonPropertyName("return_name")] public string? ReturnName { get; set; }
    [JsonPropertyName("return_phone")] public string? ReturnPhone { get; set; }
    [JsonPropertyName("return_address")] public string? ReturnAddress { get; set; }
    [JsonPropertyName("return_province_code")] public string? ReturnProvinceCode { get; set; }
    [JsonPropertyName("return_ward_code")] public string? ReturnWardCode { get; set; }
    [JsonPropertyName("r_name")] public string RName { get; set; } = "";
    [JsonPropertyName("r_phone")] public string RPhone { get; set; } = "";
    [JsonPropertyName("r_address")] public string RAddress { get; set; } = "";
    [JsonPropertyName("r_province_code")] public string RProvinceCode { get; set; } = "";
    [JsonPropertyName("r_ward_code")] public string RWardCode { get; set; } = "";
    [JsonPropertyName("is_draft")] public int? IsDraft { get; set; }
    [JsonPropertyName("other_fee")] public decimal? OtherFee { get; set; }
    [JsonPropertyName("is_installation")] public int? IsInstallation { get; set; }
    [JsonPropertyName("bill_type")] public int? BillType { get; set; }
    [JsonPropertyName("bill_return")] public string? BillReturn { get; set; }
}
