using System.ComponentModel.DataAnnotations;
using NhatTinLogistics.Sdk.Types.Responses;
using NhatTinMvc.Web.Data.Entities;

namespace NhatTinMvc.Web.Models;

/// <summary>Form tạo vận đơn (POST /v3/bill/create).</summary>
public class CreateBillViewModel
{
    public string? RefCode { get; set; }

    // Người gửi
    [Required] public string SName { get; set; } = "";
    [Required] public string SPhone { get; set; } = "";
    [Required] public string SAddress { get; set; } = "";
    public string SProvinceCode { get; set; } = "";
    public string SWardCode { get; set; } = "";

    // Người nhận
    [Required] public string RName { get; set; } = "";
    [Required] public string RPhone { get; set; } = "";
    [Required] public string RAddress { get; set; } = "";
    public string RProvinceCode { get; set; } = "";
    public string RWardCode { get; set; } = "";

    // Kiện hàng
    [Range(0.01, double.MaxValue)] public double Weight { get; set; } = 1;
    public double Width { get; set; }
    public double Length { get; set; }
    public double Height { get; set; }
    public string? CargoContent { get; set; }
    public int CargoTypeId { get; set; } = 2; // HangHoa
    public double? CargoValue { get; set; }

    // Dịch vụ / thanh toán
    public int ServiceId { get; set; } = 91;        // TietKiem
    public int PaymentMethodId { get; set; } = 11;  // SenderPayLater
    public decimal? CodAmount { get; set; }
    public string? Note { get; set; }

    // Kết quả
    public string? CreatedBillCode { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }
}

/// <summary>Form cập nhật vận đơn (POST /v3/bill/update-shipping).</summary>
public class UpdateBillViewModel
{
    [Required] public string BillCode { get; set; } = "";
    public decimal? CodAmount { get; set; }
    public double? CargoValue { get; set; }
    public double? Weight { get; set; }
    public double? Length { get; set; }
    public double? Height { get; set; }
    public double? Width { get; set; }
    public string? CargoContent { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAddress { get; set; }
    public string? Note { get; set; }

    public string? Message { get; set; }
    public bool IsError { get; set; }
}

public class BillListViewModel
{
    public IReadOnlyList<TrackedBill> Bills { get; set; } = new List<TrackedBill>();
    public string? Message { get; set; }
    public bool IsError { get; set; }
}

/// <summary>Trang chi tiết: bill đã lưu + lịch sử trạng thái (webhook/tracking) + form giả lập trạng thái.</summary>
public class BillDetailViewModel
{
    public TrackedBill Bill { get; set; } = default!;
    public IReadOnlyList<BillStatusEvent> Events { get; set; } = new List<BillStatusEvent>();
    public List<TrackingResult>? Tracking { get; set; }

    // Form giả lập trạng thái (sandbox-only)
    public int SimulateStatusId { get; set; } = 3; // PickedUp
    public string? SimulateReason { get; set; }

    public string? Message { get; set; }
    public bool IsError { get; set; }
    public string? PrintUrl { get; set; }
}
