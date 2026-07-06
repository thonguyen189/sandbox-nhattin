using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinSandbox.Infrastructure.Persistence;

public class SandboxDbContextFactory : IDesignTimeDbContextFactory<SandboxDbContext>
{
    public SandboxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SandboxDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new SandboxDbContext(options);
    }
}
