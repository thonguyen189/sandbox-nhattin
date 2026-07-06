using System.Security.Cryptography;
using System.Text;
using NhatTinSandbox.Domain.Entities;

namespace NhatTinSandbox.Infrastructure.Persistence;

public static class SeedData
{
    // Deterministic demo password hash helper (SHA-256). Sandbox only.
    public static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static void EnsureSeeded(SandboxDbContext db)
    {
        if (!db.PartnerAccounts.Any())
        {
            db.PartnerAccounts.Add(new PartnerAccount
            {
                Username = "sandbox",
                PasswordHash = Hash("sandbox123"),
                PartnerId = 123736,
                IsActive = true
            });
        }

        if (!db.WebhookSubscriptions.Any())
        {
            db.WebhookSubscriptions.Add(new WebhookSubscription
            {
                PartnerId = 123736,
                CallbackUrl = "http://localhost:5099/webhooks/nhattin/status",
                IsActive = true
            });
        }

        if (!db.MasterData.Any())
        {
            db.MasterData.AddRange(
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 90, Name = "Giao hàng nhanh (CPN)" },
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 81, Name = "Hỏa tốc" },
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 91, Name = "Tiết kiệm" },
                new MasterDataItem { Kind = MasterDataKind.Service, Code = 21, Name = "Hỗn hợp MES" },
                new MasterDataItem { Kind = MasterDataKind.PaymentMethod, Code = 10, Name = "Người gửi thanh toán ngay" },
                new MasterDataItem { Kind = MasterDataKind.PaymentMethod, Code = 11, Name = "Người gửi thanh toán sau" },
                new MasterDataItem { Kind = MasterDataKind.PaymentMethod, Code = 20, Name = "Người nhận thanh toán ngay" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 1, Name = "Chứng từ" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 2, Name = "Hàng hóa" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 3, Name = "Hàng lạnh" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 4, Name = "Sinh phẩm" },
                new MasterDataItem { Kind = MasterDataKind.CargoType, Code = 5, Name = "Mẫu bệnh phẩm" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 1, Name = "Chưa thành công", StatusCode = "Waiting" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 2, Name = "Chờ lấy hàng", StatusCode = "Waiting" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 3, Name = "Đã lấy hàng", StatusCode = "KCB" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 4, Name = "Đã giao hàng", StatusCode = "FBC" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 6, Name = "Hủy", StatusCode = "GBV" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 7, Name = "Không phát được", StatusCode = "FUD" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 9, Name = "Đang chuyển hoàn", StatusCode = "NRT" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 10, Name = "Đã chuyển hoàn", StatusCode = "MRC" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 11, Name = "Sự cố giao hàng", StatusCode = "QIU" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 12, Name = "Vận đơn nháp", StatusCode = "DRF" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 13, Name = "Đang giao hàng", StatusCode = "DEL" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 15, Name = "Đang vận chuyển" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 16, Name = "Đang giao hàng hoàn" },
                new MasterDataItem { Kind = MasterDataKind.BillStatus, Code = 17, Name = "Lỗi lấy hàng" });
        }

        if (!db.Locations.Any())
        {
            // Representative sample only (not the full national catalog).
            // Codes chosen from doc examples: provinces 01/79/11, wards 00004/25750/27007.
            db.Locations.AddRange(
                new Location { Kind = LocationKind.Province, Code = "01", Name = "Hà Nội", IsNew = true },
                new Location { Kind = LocationKind.Province, Code = "79", Name = "Hồ Chí Minh", IsNew = true },
                new Location { Kind = LocationKind.Province, Code = "11", Name = "Cao Bằng", IsNew = true },
                new Location { Kind = LocationKind.District, Code = "0101", Name = "Quận Ba Đình", ParentCode = "01", IsNew = false },
                new Location { Kind = LocationKind.District, Code = "7901", Name = "Quận 1", ParentCode = "79", IsNew = false },
                new Location { Kind = LocationKind.District, Code = "1101", Name = "TP.Cao Bằng", ParentCode = "11", IsNew = false },
                new Location { Kind = LocationKind.Ward, Code = "00004", Name = "Phường Ba Đình", ParentCode = "01", DistrictCode = "0101", IsNew = true },
                new Location { Kind = LocationKind.Ward, Code = "27007", Name = "Phường Bến Nghé", ParentCode = "79", DistrictCode = "7901", IsNew = true },
                new Location { Kind = LocationKind.Ward, Code = "25750", Name = "Phường Sài Gòn", ParentCode = "79", DistrictCode = "7901", IsNew = true });
        }

        db.SaveChanges();
    }
}
