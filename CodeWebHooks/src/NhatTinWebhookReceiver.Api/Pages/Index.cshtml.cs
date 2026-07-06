using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Domain;
using NhatTinWebhookReceiver.Api.Persistence;

namespace NhatTinWebhookReceiver.Api.Pages;

public class IndexModel : PageModel
{
    private readonly WebhookDbContext _db;
    public IndexModel(WebhookDbContext db) => _db = db;

    public List<ReceivedWebhook> Items { get; private set; } = new();

    public async Task OnGetAsync()
        => Items = await _db.ReceivedWebhooks.OrderByDescending(x => x.Id).Take(100).ToListAsync();
}
