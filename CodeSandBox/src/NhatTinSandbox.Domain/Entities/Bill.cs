namespace NhatTinSandbox.Domain.Entities;

// Fields mirror NhatTinAPIDocumentation/vi/bill/createbill.md and updatebill.md.
public class Bill
{
    public int Id { get; set; }
    public string BillCode { get; set; } = string.Empty; // Nhất Tín "bill_code" e.g. CP...
    public string? RefCode { get; set; }
    public int PackageNo { get; set; } = 1;
    public double Weight { get; set; }
    public double Width { get; set; }
    public double Length { get; set; }
    public double Height { get; set; }
    public string? CargoContent { get; set; }
    public int ServiceId { get; set; }
    public int PaymentMethodId { get; set; }
    public int IsReturnDoc { get; set; }
    public decimal CodAmount { get; set; }
    public string? Note { get; set; }
    public double CargoValue { get; set; }
    public int CargoTypeId { get; set; }

    public string SenderName { get; set; } = string.Empty;
    public string SenderPhone { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = string.Empty;
    public string SenderProvinceCode { get; set; } = string.Empty;
    public string SenderWardCode { get; set; } = string.Empty;

    public int IsReturnOrg { get; set; }
    public string? ReturnName { get; set; }
    public string? ReturnPhone { get; set; }
    public string? ReturnAddress { get; set; }
    public string? ReturnProvinceCode { get; set; }
    public string? ReturnWardCode { get; set; }

    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string ReceiverProvinceCode { get; set; } = string.Empty;
    public string ReceiverWardCode { get; set; } = string.Empty;

    public int IsDraft { get; set; }
    public decimal OtherFee { get; set; }
    public int IsInstallation { get; set; }
    public int BillType { get; set; } = 1;
    public string? BillReturn { get; set; }

    public int StatusId { get; set; } = 2; // default Chờ lấy hàng
    public decimal MainFee { get; set; }
    public decimal TotalFee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpectedAt { get; set; }

    public List<BillStatusHistory> Histories { get; set; } = new();
}
