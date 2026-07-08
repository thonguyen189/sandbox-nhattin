namespace NhatTinMvc.Web.Services;

/// <summary>
/// Helper CHỈ DÙNG CHO SANDBOX (không thuộc API NhatTin thật): gọi
/// POST /sandbox/bills/{billCode}/simulate-status để sandbox đổi trạng thái + bắn webhook về MVC.
/// Cho phép đóng trọn vòng demo ngay trong một UI.
/// </summary>
public interface ISandboxControl
{
    Task<SimulateStatusResult> SimulateStatusAsync(string billCode, int statusId, string? reason, CancellationToken ct = default);
}

public sealed record SimulateStatusResult(bool Success, string? Message, int? StatusId);
