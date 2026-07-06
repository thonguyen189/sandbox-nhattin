using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NhatTinWebhookReceiver.Api.Persistence;

public class WebhookDbContextFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    public WebhookDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseSqlite("Data Source=design-time.db").Options;
        return new WebhookDbContext(options);
    }
}
