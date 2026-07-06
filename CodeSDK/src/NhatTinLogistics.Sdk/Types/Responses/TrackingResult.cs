using System.Text.Json.Serialization;

namespace NhatTinLogistics.Sdk.Types.Responses;

/// <summary>Element of /v3/bill/tracking data. Numeric fields arrive as strings; kept as string? to avoid parse errors.</summary>
public sealed class TrackingResult
{
    [JsonPropertyName("bill_code")] public string BillCode { get; set; } = "";
    [JsonPropertyName("ref_code")] public string? RefCode { get; set; }
    [JsonPropertyName("weight")] public string? Weight { get; set; }
    [JsonPropertyName("dimension_weight")] public string? DimensionWeight { get; set; }
    [JsonPropertyName("width")] public string? Width { get; set; }
    [JsonPropertyName("length")] public string? Length { get; set; }
    [JsonPropertyName("height")] public string? Height { get; set; }
    [JsonPropertyName("payment_status")] public string? PaymentStatus { get; set; }
    [JsonPropertyName("payment_at")] public string? PaymentAt { get; set; }
    [JsonPropertyName("bill_status_id")] public int BillStatusId { get; set; }
    [JsonPropertyName("bill_status_desc")] public string? BillStatusDesc { get; set; }
    [JsonPropertyName("date_pickup")] public string? DatePickup { get; set; }
    [JsonPropertyName("pay_method")] public string? PayMethod { get; set; }
    [JsonPropertyName("service")] public string? Service { get; set; }
    [JsonPropertyName("cod_amt")] public string? CodAmount { get; set; }
    [JsonPropertyName("cod_fee")] public string? CodFee { get; set; }
    [JsonPropertyName("date_expected")] public string? DateExpected { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("cargo_content")] public string? CargoContent { get; set; }
    [JsonPropertyName("insurance_fee")] public string? InsuranceFee { get; set; }
    [JsonPropertyName("counting_fee")] public string? CountingFee { get; set; }
    [JsonPropertyName("lifting_fee")] public string? LiftingFee { get; set; }
    [JsonPropertyName("packing_fee")] public string? PackingFee { get; set; }
    [JsonPropertyName("delivery_fee")] public string? DeliveryFee { get; set; }
    [JsonPropertyName("other_fee")] public string? OtherFee { get; set; }
    [JsonPropertyName("remote_fee")] public string? RemoteFee { get; set; }
    [JsonPropertyName("main_fee")] public string? MainFee { get; set; }
    [JsonPropertyName("total_fee")] public string? TotalFee { get; set; }
    [JsonPropertyName("sender_name")] public string? SenderName { get; set; }
    [JsonPropertyName("sender_phone")] public string? SenderPhone { get; set; }
    [JsonPropertyName("sender_address")] public string? SenderAddress { get; set; }
    [JsonPropertyName("sender_ward")] public string? SenderWard { get; set; }
    [JsonPropertyName("sender_district")] public string? SenderDistrict { get; set; }
    [JsonPropertyName("sender_province")] public string? SenderProvince { get; set; }
    [JsonPropertyName("receiver_name")] public string? ReceiverName { get; set; }
    [JsonPropertyName("receiver_phone")] public string? ReceiverPhone { get; set; }
    [JsonPropertyName("receiver_address")] public string? ReceiverAddress { get; set; }
    [JsonPropertyName("receiver_ward")] public string? ReceiverWard { get; set; }
    [JsonPropertyName("receiver_district")] public string? ReceiverDistrict { get; set; }
    [JsonPropertyName("receiver_province")] public string? ReceiverProvince { get; set; }
    [JsonPropertyName("date_delivery")] public string? DateDelivery { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
    [JsonPropertyName("histories")] public List<TrackingHistory> Histories { get; set; } = new();
    [JsonPropertyName("p_link_image")] public string? PLinkImage { get; set; }
    [JsonPropertyName("bill_image_link")] public List<string> BillImageLink { get; set; } = new();
    [JsonPropertyName("document_image_link")] public List<string> DocumentImageLink { get; set; } = new();
}

public sealed class TrackingHistory
{
    [JsonPropertyName("sequence")] public int Sequence { get; set; }
    [JsonPropertyName("log_status")] public string? LogStatus { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("district")] public string? District { get; set; }
    [JsonPropertyName("operation_en")] public string? OperationEn { get; set; }
    [JsonPropertyName("operationID")] public long OperationId { get; set; }
    [JsonPropertyName("operationType")] public string? OperationType { get; set; }
    [JsonPropertyName("delayReason")] public string? DelayReason { get; set; }
    [JsonPropertyName("operation")] public string? Operation { get; set; }
    [JsonPropertyName("loc_time")] public string? LocTime { get; set; }
}
