using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NhatTinSandbox.Domain.Entities;
using NhatTinSandbox.Infrastructure.Persistence;

namespace NhatTinSandbox.AdminPortal.Pages.Bills;

public class IndexModel : PageModel
{
    private readonly SandboxDbContext _db;
    public IndexModel(SandboxDbContext db) => _db = db;

    public List<Bill> Bills { get; private set; } = new();

    public async Task OnGetAsync()
        => Bills = await _db.Bills.OrderByDescending(b => b.Id).Take(100).ToListAsync();
}
