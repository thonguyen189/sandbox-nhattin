using System.Text.Json.Serialization;
using NhatTinLogistics.Sdk.Types.Enums;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>data of /v3/bill/create and /v3/bill/update-shipping.</summary>
public sealed class BillResult
{
    [JsonPropertyName("bill_id")] public int BillId { get; set; }
    [JsonPropertyName("bill_code")] public string BillCode { get; set; } = "";
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("status_id")] public int StatusId { get; set; }
    [JsonPropertyName("cod_amount")] public decimal CodAmount { get; set; }
    [JsonPropertyName("service_id")] public int ServiceId { get; set; }
    [JsonPropertyName("payment_method")] public int PaymentMethod { get; set; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }
    [JsonPropertyName("main_fee")] public decimal MainFee { get; set; }
    [JsonPropertyName("cod_fee")] public decimal CodFee { get; set; }
    [JsonPropertyName("insurr_fee")] public decimal InsurrFee { get; set; }
    [JsonPropertyName("lifting_fee")] public decimal LiftingFee { get; set; }
    [JsonPropertyName("remote_fee")] public decimal RemoteFee { get; set; }
    [JsonPropertyName("counting_fee")] public decimal CountingFee { get; set; }
    [JsonPropertyName("packing_fee")] public decimal PackingFee { get; set; }
    [JsonPropertyName("total_fee")] public decimal TotalFee { get; set; }
    [JsonPropertyName("expected_at")] public string? ExpectedAt { get; set; }
    [JsonPropertyName("partner_address_id")] public int PartnerAddressId { get; set; }
    [JsonPropertyName("receiver_name")] public string? ReceiverName { get; set; }
    [JsonPropertyName("receiver_phone")] public string? ReceiverPhone { get; set; }
    [JsonPropertyName("receiver_address")] public string? ReceiverAddress { get; set; }
    [JsonPropertyName("package_no")] public int PackageNo { get; set; }
    [JsonPropertyName("weight")] public double Weight { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("cargo_value")] public double CargoValue { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }

    [JsonIgnore] public BillStatus Status => StatusId.ToBillStatus();
}
