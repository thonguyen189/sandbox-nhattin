using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinWebhookReceiver.Api.Persistence;

public class WebhookDbContextFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    public WebhookDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseSqlServer("Server=192.168.200.8;Database=NhatTinWebhooks;User Id=vipos;Password=CHANGE_ME;TrustServerCertificate=True;Encrypt=False").Options;
        return new WebhookDbContext(options);
    }
}
