using Microsoft.AspNetCore.Mvc;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Web.Controllers;

/// <summary>Endpoint JSON cho dropdown địa chỉ phụ thuộc (tỉnh → phường/xã). Gọi qua AJAX từ view.</summary>
public class LocationController : Controller
{
    private readonly IShippingService _shipping;
    public LocationController(IShippingService shipping) => _shipping = shipping;

    [HttpGet]
    public async Task<IActionResult> Provinces(bool isNew = true, CancellationToken ct = default)
    {
        var resp = await _shipping.GetProvincesAsync(isNew, ct);
        if (!resp.IsSuccess || resp.Data is null)
            return Json(new { success = false, message = resp.Message, items = Array.Empty<object>() });
        var items = resp.Data.Select(p => new { id = p.Id, name = p.ProvinceName });
        return Json(new { success = true, items });
    }

    [HttpGet]
    public async Task<IActionResult> Districts(string provinceId, CancellationToken ct = default)
    {
        var resp = await _shipping.GetDistrictsAsync(provinceId, ct);
        if (!resp.IsSuccess || resp.Data is null)
            return Json(new { success = false, message = resp.Message, items = Array.Empty<object>() });
        var items = resp.Data.Select(d => new { id = d.Id, name = d.DistrictName });
        return Json(new { success = true, items });
    }

    [HttpGet]
    public async Task<IActionResult> Wards(string? districtId, string? provinceId, bool isNew = true, CancellationToken ct = default)
    {
        var resp = await _shipping.GetWardsAsync(districtId, provinceId, isNew, ct);
        if (!resp.IsSuccess || resp.Data is null)
            return Json(new { success = false, message = resp.Message, items = Array.Empty<object>() });
        var items = resp.Data.Select(w => new { id = w.Id, name = w.WardName });
        return Json(new { success = true, items });
    }
}
