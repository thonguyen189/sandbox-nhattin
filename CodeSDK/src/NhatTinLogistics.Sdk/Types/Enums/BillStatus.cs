namespace NhatTinLogistics.Sdk.Types.Enums;

/// <summary>Bill status ids (Master Data §4). Unknown/unmapped ids collapse to Unknown; the raw int is kept on the DTO.</summary>
public enum BillStatus
{
    Unknown = 0,
    WaitingFail = 1,
    WaitingPickup = 2,
    PickedUp = 3,
    Delivered = 4,
    Cancelled = 6,
    FailedDelivery = 7,
    Returning = 9,
    Returned = 10,
    DeliveryIncident = 11,
    Draft = 12,
    Delivering = 13,
    InTransit = 15,
    ReturnDelivering = 16,
    PickupError = 17,
}

/// <summary>Service ids (Master Data §1).</summary>
public enum ServiceType
{
    GiaoHangNhanh = 90, // CPN
    HoaToc = 81,
    TietKiem = 91,
    HonHopMES = 21,
}

/// <summary>Payment method ids (Master Data §2).</summary>
public enum PaymentMethod
{
    SenderPayNow = 10,
    SenderPayLater = 11,
    ReceiverPayNow = 20,
}

/// <summary>Cargo type ids (Master Data §3).</summary>
public enum CargoType
{
    ChungTu = 1,
    HangHoa = 2,
    HangLanh = 3,
    SinhPham = 4,
    MauBenhPham = 5,
}

public static class BillStatusExtensions
{
    public static BillStatus ToBillStatus(this int statusId)
        => Enum.IsDefined(typeof(BillStatus), statusId) ? (BillStatus)statusId : BillStatus.Unknown;
}
