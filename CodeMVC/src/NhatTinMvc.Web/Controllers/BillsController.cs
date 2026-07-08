using Microsoft.AspNetCore.Mvc;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinMvc.Web.Models;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Web.Controllers;

public class BillsController : Controller
{
    private readonly IShippingService _shipping;
    private readonly ISandboxControl _sandbox;

    public BillsController(IShippingService shipping, ISandboxControl sandbox)
    {
        _shipping = shipping;
        _sandbox = sandbox;
    }

    // ---- Danh sách ----
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(new BillListViewModel { Bills = await _shipping.GetTrackedBillsAsync(100, ct) });

    // ---- Tạo ----
    [HttpGet]
    public IActionResult Create() => View(new CreateBillViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBillViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var req = new CreateBillRequest
        {
            RefCode = string.IsNullOrWhiteSpace(vm.RefCode) ? null : vm.RefCode,
            Weight = vm.Weight,
            Width = vm.Width,
            Length = vm.Length,
            Height = vm.Height,
            CargoContent = vm.CargoContent,
            ServiceId = vm.ServiceId,
            PaymentMethodId = vm.PaymentMethodId,
            CodAmount = vm.CodAmount,
            Note = vm.Note,
            CargoValue = vm.CargoValue,
            CargoTypeId = vm.CargoTypeId,
            SName = vm.SName, SPhone = vm.SPhone, SAddress = vm.SAddress,
            SProvinceCode = vm.SProvinceCode, SWardCode = vm.SWardCode,
            RName = vm.RName, RPhone = vm.RPhone, RAddress = vm.RAddress,
            RProvinceCode = vm.RProvinceCode, RWardCode = vm.RWardCode
        };

        var (resp, saved) = await _shipping.CreateBillAsync(req, ct);
        if (resp.IsSuccess && saved is not null)
        {
            TempData["Flash"] = $"Đã tạo vận đơn {saved.BillCode}.";
            return RedirectToAction(nameof(Details), new { id = saved.BillCode });
        }

        vm.IsError = true;
        vm.Message = resp.Message ?? "Tạo vận đơn thất bại. Đã đăng nhập chưa?";
        return View(vm);
    }

    // ---- Chi tiết + trạng thái live ----
    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var bill = await _shipping.GetTrackedBillAsync(id, ct);
        if (bill is null) return NotFound();

        var vm = new BillDetailViewModel
        {
            Bill = bill,
            Events = await _shipping.GetEventsAsync(id, ct),
            PrintUrl = SafePrintUrl(id)
        };

        // Chỉ gọi tracking khi đã đăng nhập; lỗi thì bỏ qua, vẫn hiển thị events đã lưu.
        if (_shipping.GetAuthStatus().IsAuthenticated)
        {
            try
            {
                var tr = await _shipping.TrackingAsync(id, ct);
                if (tr.IsSuccess) vm.Tracking = tr.Data;
                else vm.Message = tr.Message;
            }
            catch { /* tracking là phụ trợ; không chặn trang */ }
        }
        return View(vm);
    }

    // ---- Cập nhật ----
    [HttpGet]
    public async Task<IActionResult> Update(string id, CancellationToken ct)
    {
        var bill = await _shipping.GetTrackedBillAsync(id, ct);
        if (bill is null) return NotFound();
        return View(new UpdateBillViewModel
        {
            BillCode = bill.BillCode,
            Weight = bill.Weight,
            ReceiverName = bill.ReceiverName,
            ReceiverPhone = bill.ReceiverPhone,
            ReceiverAddress = bill.ReceiverAddress
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateBillViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);
        var req = new UpdateBillRequest
        {
            BillCode = vm.BillCode,
            CodAmount = vm.CodAmount,
            CargoValue = vm.CargoValue,
            Weight = vm.Weight,
            Length = vm.Length,
            Height = vm.Height,
            Width = vm.Width,
            CargoContent = vm.CargoContent,
            ReceiverPhone = vm.ReceiverPhone,
            ReceiverName = vm.ReceiverName,
            ReceiverAddress = vm.ReceiverAddress,
            Note = vm.Note
        };
        var resp = await _shipping.UpdateBillAsync(req, ct);
        if (resp.IsSuccess)
        {
            TempData["Flash"] = $"Đã cập nhật vận đơn {vm.BillCode}.";
            return RedirectToAction(nameof(Details), new { id = vm.BillCode });
        }
        vm.IsError = true;
        vm.Message = resp.Message ?? "Cập nhật thất bại.";
        return View(vm);
    }

    // ---- Hủy / Hoàn ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string billCode, CancellationToken ct)
    {
        var resp = await _shipping.CancelAsync(new[] { billCode }, ct);
        TempData["Flash"] = resp.IsSuccess
            ? $"Đã gửi lệnh hủy {billCode}."
            : $"Hủy thất bại: {resp.Message}";
        return RedirectToAction(nameof(Details), new { id = billCode });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revert(string billCode, CancellationToken ct)
    {
        var resp = await _shipping.RevertAsync(new[] { billCode }, ct);
        TempData["Flash"] = resp.IsSuccess
            ? $"Đã gửi lệnh hoàn {billCode}."
            : $"Hoàn thất bại: {resp.Message}";
        return RedirectToAction(nameof(Details), new { id = billCode });
    }

    // ---- In ----
    [HttpGet]
    public async Task<IActionResult> Print(string id, CancellationToken ct)
    {
        var result = await _shipping.PrintAsync(id, ct);
        if (result.Success && result.Content.Length > 0)
            return File(result.Content, result.ContentType ?? "application/octet-stream");
        TempData["Flash"] = $"Không in được {id}: {result.Message ?? result.ErrorCode ?? "lỗi không rõ"}";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Giả lập trạng thái (sandbox-only) → sandbox bắn webhook về MVC ----
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate(string billCode, int statusId, string? reason, CancellationToken ct)
    {
        var result = await _sandbox.SimulateStatusAsync(billCode, statusId, reason, ct);
        TempData["Flash"] = result.Success
            ? $"Sandbox đã đổi {billCode} → trạng thái {statusId} và bắn webhook. Chờ cập nhật live…"
            : $"Giả lập thất bại: {result.Message}";
        return RedirectToAction(nameof(Details), new { id = billCode });
    }

    private string? SafePrintUrl(string billCode)
    {
        try { return _shipping.GetPrintUrl(billCode); }
        catch { return null; } // GetPrintUrl ném nếu chưa có partner_id (chưa đăng nhập)
    }
}
