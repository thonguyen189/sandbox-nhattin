using Microsoft.EntityFrameworkCore;
using NhatTinWebhookReceiver.Api.Domain;

namespace NhatTinWebhookReceiver.Api.Persistence;

public class WebhookDbContext : DbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options) : base(options) { }
    public DbSet<ReceivedWebhook> ReceivedWebhooks => Set<ReceivedWebhook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Backstop against races: unique index on the self-dedupe key. Filtered so multiple
        // NULLs (un-dedupable events) are allowed on SQL Server. EF InMemory ignores this,
        // so the check-before-insert logic in the controller is the primary dedupe path.
        modelBuilder.Entity<ReceivedWebhook>()
            .HasIndex(w => w.DedupeKey)
            .IsUnique()
            .HasFilter("[DedupeKey] IS NOT NULL");
    }
}
