using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinSandbox.Infrastructure.Persistence;

public class SandboxDbContextFactory : IDesignTimeDbContextFactory<SandboxDbContext>
{
    public SandboxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SandboxDbContext>()
            .UseSqlServer("Server=192.168.200.8;Database=NhatTinSandbox;User Id=vipos;Password=CHANGE_ME;TrustServerCertificate=True;Encrypt=False")
            .Options;
        return new SandboxDbContext(options);
    }
}
