namespace NhatTinMvc.Web.Data.Entities;

/// <summary>
/// Một vận đơn do MVC tạo qua SDK, giữ lại để theo dõi và đối chiếu với webhook trạng thái.
/// </summary>
public class TrackedBill
{
    public int Id { get; set; }

    /// <summary>Mã vận đơn NhatTin trả về (bill_code). Unique.</summary>
    public string BillCode { get; set; } = "";
    public string? RefCode { get; set; }
    public int? PartnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public string? SenderName { get; set; }
    public string? SenderPhone { get; set; }
    public string? SenderAddress { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverAddress { get; set; }

    public double Weight { get; set; }
    public decimal TotalFee { get; set; }
    public int ServiceId { get; set; }
    public string? ServiceName { get; set; }

    /// <summary>Trạng thái mới nhất (cập nhật từ webhook hoặc tracking).</summary>
    public int LastStatusId { get; set; }
    public string? LastStatusName { get; set; }
    public DateTimeOffset? LastStatusAt { get; set; }

    /// <summary>JSON thô của phản hồi create — bằng chứng, không parse lại.</summary>
    public string? RawCreateResponse { get; set; }

    public List<BillStatusEvent> Events { get; set; } = new();
}
