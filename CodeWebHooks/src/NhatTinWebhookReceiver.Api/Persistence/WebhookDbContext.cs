using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Domain;

namespace NhatTinWebhookReceiver.Api.Persistence;

public class WebhookDbContext : DbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options) { }
    public DbSet<ReceivedWebhook> ReceivedWebhooks => Set<ReceivedWebhook>();
}
