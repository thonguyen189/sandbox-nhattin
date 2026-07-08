using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NhatTinMvc.Web.Models;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Web.Controllers;

public class HomeController : Controller
{
    private readonly IShippingService _shipping;
    private readonly IConfiguration _config;

    public HomeController(IShippingService shipping, IConfiguration config)
    {
        _shipping = shipping;
        _config = config;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new DashboardViewModel
        {
            Auth = _shipping.GetAuthStatus(),
            RecentBills = await _shipping.GetTrackedBillsAsync(10, ct),
            WebhookCallbackUrl = _config["WebhookCallbackUrl"] ?? "http://localhost:5110/webhooks/nhattin/status"
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
