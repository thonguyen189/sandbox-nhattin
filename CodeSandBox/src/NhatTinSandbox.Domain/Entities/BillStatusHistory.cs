namespace NhatTinSandbox.Domain.Entities;

public class BillStatusHistory
{
    public int Id { get; set; }
    public int BillId { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal ShippingFee { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public Bill? Bill { get; set; }
}
