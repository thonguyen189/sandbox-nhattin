using NhatTinLogistics.Sdk.Http;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinLogistics.Sdk.Types.Responses;
using NhatTinMvc.Web.Data.Entities;
using NhatTinMvc.Web.Models;

namespace NhatTinMvc.Web.Services;

/// <summary>
/// Lớp nghiệp vụ bọc SDK (Auth/Bill/Location) + lưu trữ. Controllers chỉ gọi service này, không chạm SDK
/// trực tiếp. Trả về response SDK thô (NhatTinResponse&lt;T&gt;) để controller map sang view model.
/// </summary>
public interface IShippingService
{
    // ---- Auth ----
    Task<AuthStatusViewModel> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<AuthStatusViewModel> RefreshAsync(CancellationToken ct = default);
    void Logout();
    AuthStatusViewModel GetAuthStatus();

    // ---- Location ----
    Task<NhatTinResponse<List<ProvinceDto>>> GetProvincesAsync(bool isNew, CancellationToken ct = default);
    Task<NhatTinResponse<List<DistrictDto>>> GetDistrictsAsync(string provinceId, CancellationToken ct = default);
    Task<NhatTinResponse<List<WardDto>>> GetWardsAsync(string? districtId, string? provinceId, bool isNew, CancellationToken ct = default);

    // ---- Fee ----
    Task<NhatTinResponse<List<FeeOption>>> CalcFeeAsync(CalcFeeRequest request, CancellationToken ct = default);

    // ---- Bills (ghi) ----
    /// <summary>Tạo vận đơn qua SDK; nếu thành công lưu TrackedBill và trả kèm entity đã lưu.</summary>
    Task<(NhatTinResponse<BillResult> Response, TrackedBill? Saved)> CreateBillAsync(CreateBillRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<BillResult>> UpdateBillAsync(UpdateBillRequest request, CancellationToken ct = default);
    Task<NhatTinResponse<CancelResponse>> CancelAsync(IEnumerable<string> billCodes, CancellationToken ct = default);
    Task<NhatTinResponse<RevertResult>> RevertAsync(IEnumerable<string> billCodes, CancellationToken ct = default);

    // ---- Bills (đọc / in) ----
    /// <summary>Gọi tracking; cập nhật TrackedBill.Last* từ trạng thái hiện tại (event là feed webhook, không ghi ở đây).</summary>
    Task<NhatTinResponse<List<TrackingResult>>> TrackingAsync(string billCode, CancellationToken ct = default);
    Task<PrintResult> PrintAsync(string billCode, CancellationToken ct = default);
    string GetPrintUrl(string billCode);

    // ---- Lưu trữ ----
    Task<IReadOnlyList<TrackedBill>> GetTrackedBillsAsync(int take = 100, CancellationToken ct = default);
    Task<TrackedBill?> GetTrackedBillAsync(string billCode, CancellationToken ct = default);
    Task<IReadOnlyList<BillStatusEvent>> GetEventsAsync(string billCode, CancellationToken ct = default);
}
