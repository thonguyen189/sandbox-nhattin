using NhatTinMvc.Web.Data.Entities;

namespace NhatTinMvc.Web.Models;

/// <summary>Dashboard: trạng thái đăng nhập + các vận đơn gần đây, làm điểm vào demo trọn luồng.</summary>
public class DashboardViewModel
{
    public AuthStatusViewModel Auth { get; set; } = new();
    public IReadOnlyList<TrackedBill> RecentBills { get; set; } = new List<TrackedBill>();
    public string WebhookCallbackUrl { get; set; } = "";
}
