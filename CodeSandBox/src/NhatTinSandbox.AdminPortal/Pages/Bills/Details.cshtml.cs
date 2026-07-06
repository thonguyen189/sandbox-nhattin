using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Application.Bills;
using NhatTinSandbox.Application.Webhooks;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Bills;

public class DetailsModel : PageModel
{
    private readonly SandboxDbContext _db;
    private readonly IBillService _bills;
    private readonly IWebhookDispatcher _dispatcher;

    public DetailsModel(SandboxDbContext db, IBillService bills, IWebhookDispatcher dispatcher)
    {
        _db = db; _bills = bills; _dispatcher = dispatcher;
    }

    public Bill? Bill { get; private set; }
    public List<MasterDataItem> Statuses { get; private set; } = new();
    public List<BillStatusHistory> Histories { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string BillCode { get; set; } = "";
    [BindProperty] public int NewStatusId { get; set; }
    [BindProperty] public string? Reason { get; set; }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var updated = await _bills.SetStatusAsync(BillCode, NewStatusId, Reason, HttpContext.RequestAborted);
        if (updated is not null)
            await _dispatcher.DispatchAsync(updated.BillId, HttpContext.RequestAborted);
        return RedirectToPage(new { billCode = BillCode });
    }

    private async Task LoadAsync()
    {
        Bill = await _db.Bills.FirstOrDefaultAsync(b => b.BillCode == BillCode);
        Statuses = await _db.MasterData.Where(m => m.Kind == MasterDataKind.BillStatus).OrderBy(m => m.Code).ToListAsync();
        if (Bill is not null)
            Histories = await _db.BillStatusHistories.Where(h => h.BillId == Bill.Id).OrderByDescending(h => h.Id).ToListAsync();
    }
}
