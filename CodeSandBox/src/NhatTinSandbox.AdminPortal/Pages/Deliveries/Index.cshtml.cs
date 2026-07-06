using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Deliveries;

public class IndexModel : PageModel
{
    private readonly SandboxDbContext _db;
    private readonly IWebhookDispatcher _dispatcher;
    public IndexModel(SandboxDbContext db, IWebhookDispatcher dispatcher) { _db = db; _dispatcher = dispatcher; }

    public List<WebhookDeliveryLog> Logs { get; private set; } = new();

    public async Task OnGetAsync()
        => Logs = await _db.WebhookDeliveryLogs.OrderByDescending(l => l.Id).Take(100).ToListAsync();

    public async Task<IActionResult> OnPostResendAsync(int billId)
    {
        await _dispatcher.DispatchAsync(billId, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
