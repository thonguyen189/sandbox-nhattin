using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinMvc.Web.Data;

/// <summary>
/// Design-time factory cho `dotnet ef`. Chỉ cần provider + model để sinh migration; KHÔNG kết nối DB.
/// Có factory này thì `migrations add` không chạy Program.cs (không chạm SQL runtime).
/// </summary>
public sealed class MvcDbContextFactory : IDesignTimeDbContextFactory<MvcDbContext>
{
    public MvcDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MvcDbContext>()
            .UseSqlServer("Server=.;Database=NhatTinMvc;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new MvcDbContext(options);
    }
}
