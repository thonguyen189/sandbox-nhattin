using Microsoft.AspNetCore.SignalR;

namespace NhatTinMvc.Web.Hubs;

/// <summary>Gói tin đẩy xuống browser khi một vận đơn đổi trạng thái.</summary>
public sealed record BillStatusUpdate(
    string BillCode,
    int StatusId,
    string? StatusName,
    long? StatusTime,
    DateTimeOffset ReceivedAt);

/// <summary>Hợp đồng phía client (browser) — tên method phải khớp với listener trong site.js.</summary>
public interface IBillStatusClient
{
    Task BillStatusChanged(BillStatusUpdate update);
}

/// <summary>
/// Hub đẩy trạng thái real-time. Không có method client→server; server đẩy qua
/// IHubContext&lt;BillStatusHub, IBillStatusClient&gt;. Map tại "/hubs/bill-status".
/// </summary>
public sealed class BillStatusHub : Hub<IBillStatusClient>
{
}
