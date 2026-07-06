using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Subscriptions;

public class IndexModel : PageModel
{
    private readonly SandboxDbContext _db;
    public IndexModel(SandboxDbContext db) => _db = db;

    public List<WebhookSubscription> Subscriptions { get; private set; } = new();
    [BindProperty] public string CallbackUrl { get; set; } = "";
    [BindProperty] public int PartnerId { get; set; } = 123736;

    public async Task OnGetAsync()
        => Subscriptions = await _db.WebhookSubscriptions.OrderBy(s => s.Id).ToListAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!string.IsNullOrWhiteSpace(CallbackUrl))
        {
            _db.WebhookSubscriptions.Add(new WebhookSubscription { CallbackUrl = CallbackUrl, PartnerId = PartnerId, IsActive = true });
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var sub = await _db.WebhookSubscriptions.FindAsync(id);
        if (sub is not null) { sub.IsActive = !sub.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToPage();
    }
}
