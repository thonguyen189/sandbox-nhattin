using Microsoft.AspNetCore.Mvc;
using NhatTinMvc.Web.Models;
using NhatTinMvc.Web.Services;

namespace NhatTinMvc.Web.Controllers;

public class AccountController : Controller
{
    private readonly IShippingService _shipping;
    private readonly IConfiguration _config;

    public AccountController(IShippingService shipping, IConfiguration config)
    {
        _shipping = shipping;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login()
    {
        var status = _shipping.GetAuthStatus();
        // Điền sẵn creds demo từ config cho tiện trình bày.
        var vm = new LoginViewModel
        {
            Username = _config["Sandbox:DemoUsername"] ?? "sandbox",
            Password = _config["Sandbox:DemoPassword"] ?? "sandbox123"
        };
        ViewBag.Status = status;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Status = _shipping.GetAuthStatus();
            return View(vm);
        }

        var status = await _shipping.LoginAsync(vm.Username, vm.Password, ct);
        if (status.IsError)
        {
            vm.Message = status.Message;
            vm.IsError = true;
            ViewBag.Status = status;
            return View(vm);
        }

        TempData["Flash"] = status.Message ?? "Đăng nhập thành công.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var status = await _shipping.RefreshAsync(ct);
        TempData["Flash"] = status.Message ?? (status.IsError ? "Làm mới thất bại." : "Đã làm mới token.");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _shipping.Logout();
        TempData["Flash"] = "Đã đăng xuất.";
        return RedirectToAction("Login");
    }
}
