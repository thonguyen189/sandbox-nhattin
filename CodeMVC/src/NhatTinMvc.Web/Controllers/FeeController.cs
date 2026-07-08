using Microsoft.AspNetCore.Mvc;
using NhatTinLogistics.Sdk.Types.Requests;
using NhatTinMvc.Web.Models;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Web.Controllers;

public class FeeController : Controller
{
    private readonly IShippingService _shipping;
    public FeeController(IShippingService shipping) => _shipping = shipping;

    [HttpGet]
    public IActionResult Index() => View(new FeeViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(FeeViewModel vm, CancellationToken ct)
    {
        vm.Submitted = true;
        if (!ModelState.IsValid)
            return View(vm);

        var req = new CalcFeeRequest
        {
            Weight = vm.Weight,
            Width = vm.Width,
            Length = vm.Length,
            Height = vm.Height,
            ServiceId = vm.ServiceId,
            PaymentMethodId = vm.PaymentMethodId,
            CodAmount = vm.CodAmount,
            CargoValue = vm.CargoValue,
            SProvinceId = string.IsNullOrWhiteSpace(vm.SProvinceId) ? null : vm.SProvinceId,
            SWardId = string.IsNullOrWhiteSpace(vm.SWardId) ? null : vm.SWardId,
            RProvinceId = string.IsNullOrWhiteSpace(vm.RProvinceId) ? null : vm.RProvinceId,
            RWardId = string.IsNullOrWhiteSpace(vm.RWardId) ? null : vm.RWardId
            // partner_id do SDK tự điền từ options sau khi đăng nhập.
        };

        var resp = await _shipping.CalcFeeAsync(req, ct);
        if (resp.IsSuccess && resp.Data is not null)
        {
            vm.Results = resp.Data;
            if (resp.Data.Count == 0) vm.Message = "Không có dịch vụ khả dụng cho tuyến/khối lượng này.";
        }
        else
        {
            vm.IsError = true;
            vm.Message = resp.Message ?? "Tính phí thất bại. Đã đăng nhập chưa?";
        }
        return View(vm);
    }
}
