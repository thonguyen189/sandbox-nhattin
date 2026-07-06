namespace NhatTinSandbox.Domain.Entities;

public enum MasterDataKind { Service = 1, PaymentMethod = 2, CargoType = 3, BillStatus = 4 }

public class MasterDataItem
{
    public int Id { get; set; }
    public MasterDataKind Kind { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? StatusCode { get; set; } // for BillStatus rows (e.g. KCB, FBC)
}
